using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DialogService.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DialogDbContext>
    {
        public DialogDbContext CreateDbContext(string[] args)
        {
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "7g0WfNsQziV5r5P";
            var connStr = $"Host=127.0.0.1:5432;User id=postgres;Password={password};database=socialnetwork";
            var optionsBuilder = new DbContextOptionsBuilder<DialogDbContext>();
            optionsBuilder.UseNpgsql(connStr).UseSnakeCaseNamingConvention();
            return new DialogDbContext(optionsBuilder.Options);
        }
    }
}
