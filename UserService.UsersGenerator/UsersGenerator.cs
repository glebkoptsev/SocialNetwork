using Libraries.NpgsqlService;
using Libraries.NpgsqlService.Security;
using System.Text;

namespace UserService.UsersGenerator
{
    internal class UsersGenerator(NpgsqlService npgsql)
    {
        private readonly NpgsqlService npgsql = npgsql;

        public async Task GenerateUsersAsync()
        {
            var query = await GeInsertParamsFromSourceFileAsync();
            var rowCount = await npgsql.ExecuteNonQueryAsync(query, []);
            Console.WriteLine(rowCount);
        }

        private static async Task<string> GeInsertParamsFromSourceFileAsync()
        {
            try
            {
                using StreamReader reader = new("Source/people.v2.csv");
                string text = await reader.ReadToEndAsync();
                var query = new StringBuilder();
                var lines = text.Split("\n");
                int i = 0;
                foreach (var line in lines) 
                {
                    var fields = line.Split(',');
                    var fio = fields[0].Split(' ');
                    query.AppendLine(@$"INSERT INTO public.users (user_id, first_name, second_name, birthdate, biography, city, password)
                                    VALUES ('{Guid.NewGuid()}', '{fio[1]}', '{fio[0]}', '{fields[1]}', '{line}', '{fields[2]}', '{PasswordHasher.Hash("12345")}');");
                    i++;
                    Console.WriteLine($"{i}/{lines.Length}");
                }
                return query.ToString();
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
