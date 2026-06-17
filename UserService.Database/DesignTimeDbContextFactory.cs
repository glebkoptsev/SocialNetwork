using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserService.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "7g0WfNsQziV5r5P";
            var connStr = $"Host=127.0.0.1:5432;User id=postgres;Password={password};database=socialnetwork";
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            optionsBuilder.UseNpgsql(connStr).UseSnakeCaseNamingConvention();
            return new UserDbContext(optionsBuilder.Options);
        }
    }
}
