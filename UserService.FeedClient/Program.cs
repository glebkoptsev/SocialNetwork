using Libraries.Clients.Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UserService.FeedClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var app = CreateHostBuilder(args);
            using var scope = app.Services.CreateScope();
            var feedClientSrv = scope.ServiceProvider.GetRequiredService<FeedClientSrv>();
            await feedClientSrv.WorkAsync();
        }

        public static IHost CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.Configure<UserAuthServiceOptions>(hostContext.Configuration.GetSection("AuthService"));
                    services.AddHttpClient<UserAuthService>();
                    services.AddScoped<UserAuthService>();
                    services.AddScoped<FeedClientSrv>();
                }).Build();
        }
    }
}
