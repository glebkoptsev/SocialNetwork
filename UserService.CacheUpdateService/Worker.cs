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
        private static readonly DistributedCacheEntryOptions CacheTtl = new() { SlidingExpiration = TimeSpan.FromHours(24) };

        private readonly string signalrHost = configuration["LiveFeedService:URL"]!;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await RunConsumerAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Worker outer loop error: {e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
        }

        private async Task RunConsumerAsync(CancellationToken ct)
        {
            HubConnection? connection = null;

            // Connect to SignalR
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
                        .WithAutomaticReconnect()
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

            if (connection is null || ct.IsCancellationRequested) return;

            // Connect to RabbitMQ
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
                    await channel.BasicQosAsync(0, 1, false, ct);
                    Console.WriteLine("RabbitMQ consumer connected with prefetch=1");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Попытка {attempt}/10 подключения consumer к RabbitMQ: {e.Message}");
                    if (attempt == 10) throw;
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }

            await using (connection)
            await using (rabbitConnection)
            await using (channel)
            {
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, args) =>
                {
                    try
                    {
                        var body = args.Body.ToArray();
                        var messageText = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"Обработка сообщения {messageText}");

                        var message = JsonSerializer.Deserialize<FeedUpdateMessage>(messageText, jsonOptions)!;
                        if (message.ActionType == ActionTypeEnum.FullReload)
                        {
                            Console.WriteLine($"Инициирована перезагрузка кеша");
                            await ReloadFeedAsync(message.Author_id, $"feed-{message.Author_id}", ct);
                            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                            return;
                        }

                        var authorId = message.Author_id;
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
                                    cached.RemoveAll(p => p.Post_id == message.Post_id);
                                    return cached.Count > 0 ? cached : null;
                                }, ct);

                                await NotifyFollowerAsync(connection, followerId, messageText, ct);
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
                                            cached[idx] = post;
                                            return cached;
                                        }
                                    }

                                    return null;
                                }, ct);

                                await NotifyFollowerAsync(connection, followerId, messageText, ct);
                            }
                        }

                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        var retryCount = GetRetryCount(args);
                        if (retryCount < 3)
                        {
                            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
                        }
                        else
                        {
                            Console.WriteLine($"Message dropped after {retryCount} retries: {Encoding.UTF8.GetString(args.Body.ToArray())}");
                            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                        }
                    }
                };

                await channel.BasicConsumeAsync(queue: "feed-posts", autoAck: false, consumer: consumer, cancellationToken: ct);

                // Wait for cancellation or connection loss
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
            }
        }

        private static int GetRetryCount(BasicDeliverEventArgs args)
        {
            if (args.BasicProperties?.Headers?.TryGetValue("x-retry-count", out var retryObj) == true
                && retryObj is byte[] bytes)
            {
                return BitConverter.ToInt32(bytes, 0);
            }
            return 0;
        }

        private static async Task NotifyFollowerAsync(HubConnection connection, Guid followerId, string message, CancellationToken ct)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    await connection.InvokeAsync("Send", message, followerId.ToString(), ct);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"SignalR notify failed for {followerId}: {e.Message}");
            }
        }

        private async Task ReloadFeedAsync(Guid user_id, string key, CancellationToken ct)
        {
            await using var lockHandle = await distributedLock.AcquireAsync($"lock:{key}", TimeSpan.FromSeconds(30));
            if (lockHandle is null)
            {
                Console.WriteLine($"Failed to acquire lock for {key}, skipping reload");
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
            var feedFromDb = await postRepo.GetFeedAsync(user_id, 0, 1000);
            await cache.SetStringAsync(key, JsonSerializer.Serialize(feedFromDb, jsonOptions), CacheTtl, ct);
        }

        private async Task ModifyCacheAsync(string key, Func<List<Post>, CancellationToken, Task<List<Post>?>> modify, CancellationToken ct)
        {
            var lockKey = $"lock:{key}";
            await using var handle = await distributedLock.AcquireAsync(lockKey, TimeSpan.FromSeconds(10));
            if (handle is null)
                throw new InvalidOperationException($"Failed to acquire lock for {key}");

            var cachedFeedJson = await cache.GetStringAsync(key, ct);
            List<Post> cachedFeed;
            bool wasCacheMiss = false;
            if (cachedFeedJson is null)
            {
                wasCacheMiss = true;
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
                await cache.SetStringAsync(key, JsonSerializer.Serialize(result, jsonOptions), CacheTtl, ct);
            else if (wasCacheMiss)
                await cache.SetStringAsync(key, JsonSerializer.Serialize(cachedFeed, jsonOptions), CacheTtl, ct);
        }
    }
}
