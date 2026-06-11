using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Libraries.Kafka.DTOs;
using Libraries.Kafka;
using Libraries.Web.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using UserService.Database.Entities;
using UserService.Database;
using Microsoft.AspNetCore.SignalR.Client;
using Libraries.Clients.Common;

namespace UserService.CacheUpdateService
{
    public class Worker(
        IOptions<KafkaSettings> options,
        IDistributedCache cache,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        UserAuthService userAuthService,
        IDistributedLock distributedLock) : BackgroundService
    {
        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

#if DEBUG
        private readonly string signalrHost = configuration["LiveFeedService:URL_Debug"]!;
#else
        private readonly string signalrHost = configuration["LiveFeedService:URL"]!;
#endif

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            HubConnection? connection = null;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var token = await userAuthService.GetTokenAsync();
                    if (token is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), ct);
                        continue;
                    }

                    connection = new HubConnectionBuilder()
                        .WithUrl(signalrHost, x => x.AccessTokenProvider = () => Task.FromResult<string?>(token))
                        .Build();
                    await connection.StartAsync(ct);
                    break;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            }

            await using (connection!)
            {
                await EnsureTopicExistsAsync(ct);

                using var consumer = new ConsumerBuilder<string, string>(GetConsumerConfig()).Build();
                consumer.Subscribe("feed-posts");
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var consumerResult = consumer.Consume(ct);
                        if (consumerResult.IsPartitionEOF)
                        {
                            await Task.Delay(2000, ct);
                            continue;
                        }
                        Console.WriteLine($"Обработка сообщения {consumerResult.Message.Value}; Ключ {consumerResult.Message.Key}");

                        var message = JsonSerializer.Deserialize<FeedUpdateMessage>(consumerResult.Message.Value, jsonOptions)!;
                        if (message.ActionType == ActionTypeEnum.FullReload)
                        {
                            Console.WriteLine($"Инициирована перезагрузка кеша");
                            await ReloadFeedAsync(message.Author_id, $"feed-{consumerResult.Message.Key}", ct);
                            consumer.StoreOffset(consumerResult);
                            continue;
                        }
                        else
                        {
                            await connection.InvokeAsync("Send", consumerResult.Message.Value, consumerResult.Message.Key, ct);
                        }

                        var user_id = Guid.Parse(consumerResult.Message.Key);
                        if (message.Post_id is null)
                            throw new Exception($"Invalid message with actiontype = {message.ActionType} and post_id = null");
                        Console.WriteLine($"Обработка юзера {user_id}");
                        var key = $"feed-{user_id}";

                        if (message.ActionType == ActionTypeEnum.Delete)
                        {
                            await ModifyCacheAsync(key, async (cached, _) =>
                            {
                                Console.WriteLine($"Удаление поста {message.Post_id}");
                                cached.RemoveAll(p => p.Post_id == message.Post_id);
                                return cached.Count > 0 ? cached : null;
                            }, ct);
                        }
                        else
                        {
                            using var scope = scopeFactory.CreateScope();
                            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
                            var post = await postRepo.GetPostAsync(message.Post_id.Value)
                                ?? throw new Exception($"post {message.Post_id.Value} not found in db");

                            await ModifyCacheAsync(key, async (cached, _) =>
                            {
                                if (message.ActionType == ActionTypeEnum.Create && cached.All(f => f.Post_id != message.Post_id.Value))
                                {
                                    Console.WriteLine($"Добавление поста {message.Post_id}");
                                    cached.Add(post);
                                    if (cached.Count > 1000)
                                    {
                                        var oldestPost = cached.MinBy(f => f.Creation_datetime)!;
                                        cached.Remove(oldestPost);
                                    }
                                    return cached;
                                }

                                if (message.ActionType == ActionTypeEnum.Update)
                                {
                                    var idx = cached.FindIndex(f => f.Post_id == message.Post_id.Value);
                                    if (idx >= 0)
                                    {
                                        Console.WriteLine($"Обновление поста {message.Post_id}");
                                        cached[idx] = post;
                                        return cached;
                                    }
                                }

                                return null;
                            }, ct);
                        }
                        consumer.StoreOffset(consumerResult);

                    }
                    catch (Exception e)
                    {
                        if (e is TaskCanceledException || e is OperationCanceledException)
                        {
                            Console.WriteLine("shut down");
                            break;
                        }

                        Console.WriteLine(e.ToString());
                    }
                }
                consumer.Close();
            }
        }

        private async Task ReloadFeedAsync(Guid user_id, string key, CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
            var feedFromDb = await postRepo.GetFeedAsync(user_id, 0, 1000);
            await cache.SetStringAsync(key, JsonSerializer.Serialize(feedFromDb, jsonOptions), ct);
        }

        private async Task ModifyCacheAsync(string key, Func<List<Post>, CancellationToken, Task<List<Post>?>> modify, CancellationToken ct)
        {
            var lockKey = $"lock:{key}";
            await using var handle = await distributedLock.AcquireAsync(lockKey, TimeSpan.FromSeconds(10));
            if (handle is null)
            {
                Console.WriteLine($"Failed to acquire lock for {key}, skipping");
                return;
            }

            var cachedFeedJson = await cache.GetStringAsync(key, ct);
            List<Post> cachedFeed;
            if (cachedFeedJson is null)
            {
                Console.WriteLine($"Cache miss for {key}, reloading from DB");
                using var scope = scopeFactory.CreateScope();
                var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
                var userId = Guid.Parse(key.AsSpan(5));
                cachedFeed = await postRepo.GetFeedAsync(userId, 0, 1000);
            }
            else
            {
                cachedFeed = JsonSerializer.Deserialize<List<Post>>(cachedFeedJson, jsonOptions)!;
            }

            var result = await modify(cachedFeed, ct);
            if (result is not null)
                await cache.SetStringAsync(key, JsonSerializer.Serialize(result, jsonOptions), ct);
        }

        private async Task EnsureTopicExistsAsync(CancellationToken ct)
        {
            var topic = "feed-posts";
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    using var admin = new AdminClientBuilder(new AdminClientConfig
                    {
#if DEBUG
                        BootstrapServers = options.Value.Host_debug,
#else
                        BootstrapServers = options.Value.Host,
#endif
                    }).Build();
                    await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = (short)1 }], new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });
                    Console.WriteLine($"Топик {topic} создан");
                    return;
                }
                catch (CreateTopicsException e) when (e.Error.Code == ErrorCode.TopicAlreadyExists)
                {
                    Console.WriteLine($"Топик {topic} уже существует");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Попытка {attempt}/10 создать топик {topic}: {e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }
            Console.WriteLine($"Не удалось создать топик {topic} после 10 попыток");
        }

        private ConsumerConfig GetConsumerConfig()
        {
            return new ConsumerConfig
            {
                GroupId = "CaseUpdateService",
                EnableAutoOffsetStore = false,
                EnableAutoCommit = true,
                EnablePartitionEof = true,
                AutoOffsetReset = AutoOffsetReset.Earliest,
#if DEBUG
                BootstrapServers = options.Value.Host_debug,
#else
                BootstrapServers = options.Value.Host,
#endif

            };
        }
    }
}
