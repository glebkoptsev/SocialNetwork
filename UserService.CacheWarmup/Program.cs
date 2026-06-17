using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserService.Database;

namespace UserService.CacheWarmup
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var app = CreateHostBuilder(args);
            using var scope = app.Services.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<CacheWarmuper>();
            await generator.WarmupAsync();
        }

        public static IHost CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContextFactory<UserDbContext>(options =>
                    {
                        var connStr = hostContext.Configuration["ConnectionStrings:postgres"];
                        options.UseNpgsql(connStr!).UseSnakeCaseNamingConvention();
                    });
                    services.AddScoped<CacheWarmuper>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = hostContext.Configuration.GetConnectionString("redis");
                    });
                }).Build();
        }
    }
}
