using Libraries.Clients.Common;
using Libraries.Kafka;
using Libraries.Web.Common.Caching;
using Microsoft.EntityFrameworkCore;
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
                    services.AddDbContextFactory<UserDbContext>(options =>
                    {
                        var connStr = hostContext.Configuration.GetConnectionString("postgres");
                        options.UseNpgsql(connStr!).UseSnakeCaseNamingConvention();
                    });
                    services.AddTransient<IFeedOutboxStore, FeedOutboxStore>();
                    services.AddTransient<IPostRepository, PostRepository>();
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

            // Apply migrations
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
                await context.Database.MigrateAsync();
            }

            await host.RunAsync();
        }
    }
}
