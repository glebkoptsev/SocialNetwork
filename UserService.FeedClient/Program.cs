using Microsoft.AspNetCore.SignalR.Client;

namespace UserService.FeedClient
{
    internal class Program
    {
        static async Task Main()
        {
            await using var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5125/post/feed/posted", x => x.AccessTokenProvider = async ()
                        => await Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxMmIyN2M1YS1iM2ZkLTRjM2ItYjA3NC1hOGU2NDNjOTEwYjIiLCJ1bmlxdWVfbmFtZSI6InN0cmluZyIsIm5iZiI6MTcyNjc3Njk1MCwiZXhwIjoxNzI2ODYzMzUwLCJpYXQiOjE3MjY3NzY5NTAsImlzcyI6ImlzcyIsImF1ZCI6ImF1ZCJ9.4SWG4Q04cG1oAVRxdq0ZUK_SfrMiiPz-jHGa7rVbEHM"))
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
