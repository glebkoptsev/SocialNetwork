using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.CacheWarmup
{
    internal class CacheWarmuper(IDbContextFactory<UserDbContext> contextFactory, IDistributedCache distributedCache)
    {
        private readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task WarmupAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var users_with_friends = await context.Friends
                .Select(f => f.User_id)
                .Distinct()
                .ToListAsync();

            foreach (var user_id in users_with_friends)
            {
                var user_feed = await GetFeedAsync(context, user_id);
                if (user_feed.Count == 0) continue;

                string key = $"feed-{user_id}";
                var cached_feed_json = await distributedCache.GetStringAsync(key);
                var user_feed_json = JsonSerializer.Serialize(user_feed, jsonSerializerOptions);
                if (cached_feed_json == user_feed_json)
                {
                    await distributedCache.RefreshAsync(key);
                }
                else
                {
                    await distributedCache.SetStringAsync(key, user_feed_json, new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(24)
                    });
                }
            }
            Console.WriteLine("the end");
        }

        private static async Task<List<Post>> GetFeedAsync(UserDbContext context, Guid user_id)
        {
            var posts = await (from f in context.Friends
                               join p in context.Posts on f.Friend_id equals p.User_id
                               join u in context.Users on p.User_id equals u.User_id
                               where f.User_id == user_id
                               orderby p.Creation_datetime descending
                               select new Post
                               {
                                   Post_id = p.Post_id,
                                   User_id = p.User_id,
                                   Text = p.Text,
                                   Creation_datetime = p.Creation_datetime,
                                   AuthorFirstName = u.First_name,
                                   AuthorSecondName = u.Second_name
                               })
                               .Take(1000)
                               .ToListAsync();
            return posts;
        }
    }
}
