using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserService.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
            var connStr = "Host=127.0.0.1:5432;User id=postgres;Password=7g0WfNsQziV5r5P;database=socialnetwork";
            optionsBuilder.UseNpgsql(connStr).UseSnakeCaseNamingConvention();
            return new UserDbContext(optionsBuilder.Options);
        }
    }
}
