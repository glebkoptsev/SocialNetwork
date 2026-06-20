using Microsoft.EntityFrameworkCore;

namespace DialogService.Database
{
    public class DialogReadDbContext(DbContextOptions<DialogReadDbContext> options) : DialogDbContext(options)
    {
    }
}
