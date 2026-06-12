using Libraries.Web.Common.Security;
using Microsoft.EntityFrameworkCore;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.UsersGenerator
{
    internal class UsersGenerator(IDbContextFactory<UserDbContext> contextFactory)
    {
        private const int BatchSize = 1000;

        public async Task GenerateUsersAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            int total = 0;
            var users = new List<User>(BatchSize);

            using var reader = new StreamReader("Source/people.v2.csv");
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.Split(',');
                var fio = fields[0].Split(' ');
                users.Add(new User
                {
                    User_id = Guid.NewGuid(),
                    First_name = fio[1],
                    Second_name = fio[0],
                    Birthdate = fields[1],
                    Biography = line,
                    City = fields[2],
                    Password = PasswordHasher.Hash("12345"),
                    Login = $"user_{Guid.NewGuid():N}"
                });

                if (users.Count >= BatchSize)
                {
                    await FlushBatchAsync(context, users);
                    total += users.Count;
                    Console.WriteLine($"{total}");
                    users.Clear();
                }
            }

            if (users.Count > 0)
            {
                await FlushBatchAsync(context, users);
                total += users.Count;
            }

            Console.WriteLine($"Done: {total} users inserted");
        }

        private static async Task FlushBatchAsync(UserDbContext context, List<User> users)
        {
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }
}
