using Microsoft.EntityFrameworkCore;

namespace UserService.Database
{
    public class UserReadDbContext(DbContextOptions<UserReadDbContext> options) : UserDbContext(options)
    {
    }
}
