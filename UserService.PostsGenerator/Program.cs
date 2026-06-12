using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserService.Database;

namespace UserService.PostsGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var app = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var connStr = hostContext.Configuration["ConnectionStrings:postgres"];
                    services.AddDbContextFactory<UserDbContext>(options =>
                        options.UseNpgsql(connStr!).UseSnakeCaseNamingConvention());
                    services.AddScoped<PostGenerator>();
                }).Build();

            using var scope = app.Services.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<PostGenerator>();
            await generator.GeneratePostsAsync();
        }
    }
}
