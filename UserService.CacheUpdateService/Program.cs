using Libraries.Clients.Common;
using Libraries.Kafka;
using Libraries.NpgsqlService;
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
                    services.AddSingleton<NpgsqlService>();
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
                });

            var host = builder.Build();
            await host.RunAsync();
        }
    }
}