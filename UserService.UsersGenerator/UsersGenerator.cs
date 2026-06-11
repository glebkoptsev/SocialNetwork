using Libraries.Web.Common.Security;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.UsersGenerator
{
    internal class UsersGenerator(UserDbContext context)
    {
        public async Task GenerateUsersAsync()
        {
            var users = await GetUsersFromSourceFileAsync();
            foreach (var user in users)
            {
                context.Users.Add(user);
            }
            var rowCount = await context.SaveChangesAsync();
            Console.WriteLine(rowCount);
        }

        private static async Task<List<User>> GetUsersFromSourceFileAsync()
        {
            var users = new List<User>();
            try
            {
                using StreamReader reader = new("Source/people.v2.csv");
                string text = await reader.ReadToEndAsync();
                var lines = text.Split("\n");
                int i = 0;
                foreach (var line in lines)
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
                    i++;
                    Console.WriteLine($"{i}/{lines.Length}");
                }
                return users;
            }
            catch (IOException e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
