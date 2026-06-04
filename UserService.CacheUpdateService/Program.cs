using Libraries.Clients.Common;
using Libraries.Kafka;
using Libraries.NpgsqlService;
using Libraries.Web.Common.Caching;
using StackExchange.Redis;
using UserService.Database;

namespace UserService.CacheUpdateService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.Configure<KafkaSettings>(hostContext.Configuration.GetSection("KafkaSettings"));
                    services.Configure<UserAuthServiceOptions>(hostContext.Configuration.GetSection("AuthService"));
                    services.AddSingleton<INpgsqlService, NpgsqlService>();
                    services.AddSingleton<IFeedOutboxStore, FeedOutboxStore>();
                    services.AddSingleton<IConnectionMultiplexer>(_ =>
                    {
                        var connStr = hostContext.Configuration.GetConnectionString("redis");
                        return ConnectionMultiplexer.Connect(connStr!);
                    });
                    services.AddSingleton<IDistributedLock, RedisLock>();
                    services.AddSingleton<IKafkaProducer, KafkaProducer<string, string>>();
                    services.AddSingleton<KafkaClientHandle>();
                    services.AddHttpClient<UserAuthService>();
                    services.AddSingleton<UserAuthService>();
                    services.AddTransient<PostRepository>();
                    services.AddStackExchangeRedisCache(options =>
                    {
#if DEBUG
                        options.Configuration = hostContext.Configuration.GetConnectionString("redis_debug");
#else
                        options.Configuration = hostContext.Configuration.GetConnectionString("redis");
#endif
                    });
                    services.AddHostedService<Worker>();
                    services.AddHostedService<OutboxPublisher>();
                });

            var host = builder.Build();

            // Ensure feed_outbox table exists
            var npgsql = host.Services.GetRequiredService<INpgsqlService>();
            await npgsql.ExecuteNonQueryAsync("""
                CREATE TABLE IF NOT EXISTS public.feed_outbox (
                    id BIGSERIAL PRIMARY KEY,
                    kafka_key TEXT NOT NULL,
                    kafka_value TEXT NOT NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    processed_at TIMESTAMPTZ
                )
                """, []);

            await host.RunAsync();
        }
    }
}