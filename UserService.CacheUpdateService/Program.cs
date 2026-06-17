using Libraries.Clients.Common;
using Libraries.RabbitMQ;
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
                    services.Configure<RabbitMQSettings>(hostContext.Configuration.GetSection("RabbitMQSettings"));
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
                    services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();
                    services.AddSingleton<RabbitMQInitializer>();
                    services.AddHttpClient<UserAuthService>();
                    services.AddSingleton<UserAuthService>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = hostContext.Configuration.GetConnectionString("redis");
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

            // Initialize RabbitMQ topology
            var rabbitInit = host.Services.GetRequiredService<RabbitMQInitializer>();
            await rabbitInit.InitializeAsync();

            await host.RunAsync();
        }
    }
}
