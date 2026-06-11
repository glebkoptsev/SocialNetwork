using Microsoft.EntityFrameworkCore;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.PostsGenerator
{
    internal class PostGenerator(UserDbContext context)
    {
        public async Task GeneratePostsAsync()
        {
            var posts = await GetPostsFromSourceFileAsync();
            var allUserIds = await context.Users.Select(u => u.User_id).ToListAsync();
            var rnd = new Random();
            int cnt = 0;

            foreach (var userId in allUserIds)
            {
                var postsCount = rnd.Next(1, 50);
                for (int i = 0; i < postsCount; i++)
                {
                    var text = posts[rnd.Next(0, posts.Length - 1)].Trim();
                    context.Posts.Add(new Post
                    {
                        Post_id = Guid.NewGuid(),
                        User_id = userId,
                        Text = text,
                        Creation_datetime = DateTime.UtcNow
                    });
                }

                if (cnt % 500 == 0)
                {
                    await context.SaveChangesAsync();
                }
                cnt++;
                Console.WriteLine($"{cnt}/{allUserIds.Count}");
            }

            await context.SaveChangesAsync();
            Console.WriteLine(allUserIds.Count);
        }

        private static async Task<string[]> GetPostsFromSourceFileAsync()
        {
            try
            {
                using StreamReader reader = new("Source/posts.txt");
                string text = await reader.ReadToEndAsync();
                return text.Split(".");
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
