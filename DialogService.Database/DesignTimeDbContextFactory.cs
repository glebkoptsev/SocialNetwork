using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DialogService.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DialogDbContext>
    {
        public DialogDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DialogDbContext>();
            var connStr = "Host=127.0.0.1:5432;User id=postgres;Password=7g0WfNsQziV5r5P;database=socialnetwork";
            optionsBuilder.UseNpgsql(connStr).UseSnakeCaseNamingConvention();
            return new DialogDbContext(optionsBuilder.Options);
        }
    }
}
