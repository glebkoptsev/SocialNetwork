using Microsoft.EntityFrameworkCore;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.PostsGenerator
{
    internal class PostGenerator(IDbContextFactory<UserDbContext> contextFactory)
    {
        private const int BatchSize = 500;

        public async Task GeneratePostsAsync()
        {
            var posts = await GetPostsFromSourceFileAsync();

            await using var context = await contextFactory.CreateDbContextAsync();
            context.ChangeTracker.AutoDetectChangesEnabled = false;

            var allUserIds = await context.Users
                .Where(u => u.Login != "system")
                .Select(u => u.User_id)
                .ToListAsync();

            var rnd = new Random();
            int totalPosts = 0;
            int processed = 0;
            int count = 0;

            foreach (var userId in allUserIds)
            {
                var postsCount = rnd.Next(1, 201);
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
                    count++;
                    totalPosts++;
                }

                if (count >= BatchSize)
                {
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear();
                    count = 0;
                }

                processed++;
                Console.WriteLine($"{processed}/{allUserIds.Count} users, {totalPosts} posts");
            }

            if (count > 0)
                await context.SaveChangesAsync();

            Console.WriteLine($"Done: {totalPosts} posts for {processed} users");
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
