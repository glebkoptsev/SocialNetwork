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
    public class Worker : BackgroundService
    {
        private readonly IOptions<RabbitMQSettings> options;
        private readonly IDistributedCache cache;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly UserAuthService userAuthService;
        private readonly IDistributedLock distributedLock;
        private readonly ILogger<Worker> logger;
        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly DistributedCacheEntryOptions CacheTtl = new() { SlidingExpiration = TimeSpan.FromHours(24) };
        private readonly string signalrHost;

        public Worker(
            IOptions<RabbitMQSettings> options,
            IDistributedCache cache,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            UserAuthService userAuthService,
            IDistributedLock distributedLock,
            ILogger<Worker> logger)
        {
            this.options = options;
            this.cache = cache;
            this.scopeFactory = scopeFactory;
            this.userAuthService = userAuthService;
            this.distributedLock = distributedLock;
            this.logger = logger;
            signalrHost = configuration["SignalR:HubUrl"]!;
        }

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
                    logger.LogWarning(e, "Worker outer loop error, restarting in 5s");
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
        }

        private async Task RunConsumerAsync(CancellationToken ct)
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
                        .WithAutomaticReconnect()
                        .Build();
                    await connection.StartAsync(ct);
                    logger.LogInformation("Connected to SignalR hub");
                    break;
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "SignalR connection failed, retry in 10s");
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
            }

            if (connection is null || ct.IsCancellationRequested) return;

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            };

            IConnection rabbitConnection = null!;
            IChannel channel = null!;
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    rabbitConnection = await factory.CreateConnectionAsync();
                    channel = await rabbitConnection.CreateChannelAsync();
                    await channel.BasicQosAsync(0, 1, false, ct);
                    logger.LogInformation("RabbitMQ consumer connected with prefetch=1");
                    break;
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "RabbitMQ connection attempt {Attempt}/10", attempt);
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

                        var message = JsonSerializer.Deserialize<FeedUpdateMessage>(messageText, jsonOptions)!;
                        if (message.ActionType == ActionTypeEnum.FullReload)
                        {
                            logger.LogInformation("FullReload for user {UserId}", message.Author_id);
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
                        logger.LogError(e, "Message processing failed");
                        var retryCount = GetRetryCount(args);
                        if (retryCount < 3)
                        {
                            var props = new BasicProperties
                            {
                                Headers = new Dictionary<string, object?>
                                {
                                    ["x-retry-count"] = BitConverter.GetBytes(retryCount + 1)
                                }
                            };
                            await channel.BasicPublishAsync(
                                exchange: "feed-posts",
                                routingKey: "feed-posts",
                                mandatory: false,
                                basicProperties: props,
                                body: args.Body.ToArray(),
                                cancellationToken: ct);
                            logger.LogWarning("Re-queued message with retry {Retry}", retryCount + 1);
                        }
                        else
                        {
                            logger.LogWarning("Message dropped after {Retry} retries", retryCount);
                        }
                        await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: ct);
                    }
                };

                await channel.BasicConsumeAsync(queue: "feed-posts", autoAck: false, consumer: consumer, cancellationToken: ct);
                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { }
            }
        }

        private static int GetRetryCount(BasicDeliverEventArgs args)
        {
            if (args.BasicProperties?.Headers?.TryGetValue("x-retry-count", out var retryObj) == true
                && retryObj is byte[] bytes)
                return BitConverter.ToInt32(bytes, 0);
            return 0;
        }

        private static async Task NotifyFollowerAsync(HubConnection connection, Guid followerId, string message, CancellationToken ct)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                    await connection.InvokeAsync("Send", message, followerId.ToString(), ct);
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
                logger.LogWarning("Failed to acquire lock for {Key}, skipping reload", key);
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
