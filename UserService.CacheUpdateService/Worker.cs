using Libraries.RabbitMQ;
using Libraries.Web.Common.Caching;
using Libraries.Web.Common.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UserService.Database;
using UserService.Database.Entities;
using Libraries.Clients.Common;

namespace UserService.CacheUpdateService
{
    public class Worker(
        IOptions<RabbitMQSettings> options,
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
                    Console.WriteLine("Connected to SignalR hub");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"SignalR connection failed: {e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            }

            await using (connection!)
            {
                var settings = options.Value;
                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.Username,
                    Password = settings.Password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                };

                Console.WriteLine("Connecting RabbitMQ consumer...");
                IConnection rabbitConnection = null!;
                IChannel channel = null!;
                for (int attempt = 1; attempt <= 10; attempt++)
                {
                    try
                    {
                        rabbitConnection = await factory.CreateConnectionAsync();
                        channel = await rabbitConnection.CreateChannelAsync();
                        Console.WriteLine("RabbitMQ consumer connected");
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Попытка {attempt}/10 подключения consumer к RabbitMQ: {e.Message}");
                        if (attempt == 10) throw;
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                    }
                }

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, args) =>
                {
                    try
                    {
                        var body = args.Body.ToArray();
                        var messageText = Encoding.UTF8.GetString(body);
                        var key = args.BasicProperties?.MessageId ?? args.RoutingKey;
                        Console.WriteLine($"Обработка сообщения {messageText}; Ключ {key}");

                        var message = JsonSerializer.Deserialize<FeedUpdateMessage>(messageText, jsonOptions)!;
                        if (message.ActionType == ActionTypeEnum.FullReload)
                        {
                            Console.WriteLine($"Инициирована перезагрузка кеша");
                            await ReloadFeedAsync(message.Author_id, $"feed-{message.Author_id}", ct);
                            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                            return;
                        }

                        await connection!.InvokeAsync("Send", messageText, key, ct);

                        var authorId = message.Author_id;
                        Console.WriteLine($"Обработка автора {authorId}");
                        using var scope = scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
                        var followerIds = await dbContext.Friends
                            .Where(f => f.Friend_id == authorId)
                            .Select(f => f.User_id)
                            .ToListAsync(ct);

                        if (followerIds.Count == 0)
                        {
                            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                            return;
                        }

                        if (message.ActionType == ActionTypeEnum.Delete)
                        {
                            foreach (var followerId in followerIds)
                            {
                                await ModifyCacheAsync($"feed-{followerId}", async (cached, _) =>
                                {
                                    Console.WriteLine($"Удаление поста {message.Post_id}");
                                    cached.RemoveAll(p => p.Post_id == message.Post_id);
                                    return cached.Count > 0 ? cached : null;
                                }, ct);
                            }
                        }
                        else
                        {
                            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
                            var post = await postRepo.GetPostAsync(message.Post_id!.Value)
                                ?? throw new Exception($"post {message.Post_id.Value} not found in db");

                            foreach (var followerId in followerIds)
                            {
                                await ModifyCacheAsync($"feed-{followerId}", async (cached, _) =>
                                {
                                    if (message.ActionType == ActionTypeEnum.Create && cached.All(f => f.Post_id != message.Post_id.Value))
                                    {
                                        Console.WriteLine($"Добавление поста {message.Post_id} для {followerId}");
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
                                            Console.WriteLine($"Обновление поста {message.Post_id} для {followerId}");
                                            cached[idx] = post;
                                            return cached;
                                        }
                                    }

                                    return null;
                                }, ct);
                            }
                        }

                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
                    }
                };

                await channel.BasicConsumeAsync(queue: "feed-posts", autoAck: false, consumer: consumer, cancellationToken: ct);

                await Task.Delay(Timeout.Infinite, ct);

                await channel.DisposeAsync();
                await rabbitConnection.DisposeAsync();
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
            bool wasCacheMiss = false;
            if (cachedFeedJson is null)
            {
                wasCacheMiss = true;
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
            else if (wasCacheMiss)
                await cache.SetStringAsync(key, JsonSerializer.Serialize(cachedFeed, jsonOptions), ct);
        }
    }
}
