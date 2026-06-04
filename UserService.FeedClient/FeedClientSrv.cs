using Libraries.Clients.Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace UserService.FeedClient
{
    public class FeedClientSrv(UserAuthService userAuthService, IConfiguration configuration) : BackgroundService
    {
        private readonly UserAuthService userAuthService = userAuthService;
#if DEBUG
        private readonly string signalrHost = configuration["LiveFeedService:URL_Debug"]!;
#else
        private readonly string signalrHost = configuration["LiveFeedService:URL"]!;
#endif

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var token = await userAuthService.GetTokenAsync();
                    if (token is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    await using var connection = new HubConnectionBuilder()
                        .WithUrl(signalrHost, x => x.AccessTokenProvider = () => Task.FromResult<string?>(token))
                        .WithAutomaticReconnect()
                        .Build();

                    connection.On<string>("Receive", (message) =>
                    {
                        Console.WriteLine(message);
                    });
                    await connection.StartAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
    }
}
