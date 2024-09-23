using Libraries.Clients.Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace UserService.FeedClient
{
    public class FeedClientSrv(UserAuthService userAuthService, IConfiguration configuration)
    {
        private readonly UserAuthService userAuthService = userAuthService;
#if DEBUG
        private readonly string signalrHost = configuration["LiveFeedService:URL_Debug"]!;
#else
        private readonly string signalrHost = configuration["LiveFeedService:URL"]!;
#endif

        public async Task WorkAsync()
        {
            await using var connection = new HubConnectionBuilder()
                .WithUrl(signalrHost, x => x.AccessTokenProvider = async ()
                        => await userAuthService.GetTokenAsync())
                .WithAutomaticReconnect()
                .Build();

            connection.On<string>("Receive", (message) =>
            {
                Console.WriteLine(message);
            });
            await connection.StartAsync();
            Console.Read();
            Console.WriteLine("end");
        }
    }
}
