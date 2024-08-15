using System.Text;
using Libraries.NpgsqlService;

namespace UserService.PostsGenerator
{
    internal class PostGenerator(NpgsqlService npgsql)
    {
        private readonly NpgsqlService npgsql = npgsql;

        public async Task GeneratePostsAsync()
        {
            var posts = await GePostsFromSourceFileAsync();
            var all_users_ids = await npgsql.GetQueryResultAsync("select user_id from users", [], ["user_id"]);
            Random rnd = new();
            var sb = new StringBuilder();
            int cnt = 0;
            foreach (var user_id in all_users_ids)
            {
                var posts_count = rnd.Next(1, 50);
                for (int i = 0; i < posts_count; i++) 
                {
                    var text = posts[rnd.Next(0, posts.Length - 1)].Trim();
                    sb.Append($"insert into public.posts (post_id, user_id, post) values ('{Guid.NewGuid()}', '{user_id["user_id"]}', '{text}');\n");
                }
                if (cnt % 5000 == 0)
                {
                    await npgsql.ExecuteNonQueryAsync(sb.ToString(), []);
                    sb.Clear();
                }
                cnt++;
                Console.WriteLine($"{cnt}/{all_users_ids.Count}");
            }
            await npgsql.ExecuteNonQueryAsync(sb.ToString(), []);
            sb.Clear();
            //var rowCount = await npgsql.ExecuteNonQueryAsync(query, []);
            Console.WriteLine(all_users_ids.Count);
        }

        private static async Task<string[]> GePostsFromSourceFileAsync()
        {
            try
            {
                using StreamReader reader = new("Source/posts.txt");
                string text = await reader.ReadToEndAsync();
                var query = new StringBuilder();
                var lines = text.Split(".");
                return lines;
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
