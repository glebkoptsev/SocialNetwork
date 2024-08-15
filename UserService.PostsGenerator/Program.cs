using Libraries.NpgsqlService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UserService.PostsGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var app = CreateHostBuilder(args);
            using var scope = app.Services.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<PostGenerator>();
            await generator.GeneratePostsAsync();
        }

        public static IHost CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<NpgsqlService>();
                    services.AddScoped<PostGenerator>();
                }).Build();
        }
    }
}
