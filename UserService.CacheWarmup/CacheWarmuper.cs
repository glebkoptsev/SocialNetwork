using Libraries.NpgsqlService;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using UserService.Database.Entities;

namespace UserService.СacheWarmup
{
    internal class CacheWarmuper(NpgsqlService npgsql, IDistributedCache distributedCache)
    {
        private readonly NpgsqlService npgsql = npgsql;
        private readonly IDistributedCache distributedCache = distributedCache;
        private readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task WarmupAsync()
        {
            var users_with_friends = await GetUsersWithFriendsAsync();
            foreach (var user_id in users_with_friends)
            {
                var user_feed = await GetFeedAsync(user_id);
                if (user_feed.Count == 0)
                {
                    continue;
                }
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

        private async Task<List<Guid>> GetUsersWithFriendsAsync()
        {
            string query = @"select distinct user_id from friends";
            var data = await npgsql.GetQueryResultAsync(query, [], ["user_id"], TargetSessionAttributes.PreferStandby);
            if (data.Count == 0) return [];
            var posts = new List<Guid>();
            foreach (var row in data)
            {
                posts.Add(Guid.Parse(row["user_id"].ToString()!));
            }
            return posts;
        }

        private async Task<List<Post>> GetFeedAsync(Guid user_id)
        {
            string query = @"select p.user_id, p.post_id, p.creation_datetime, p.post 
                             from friends f
                             inner join posts p on f.friend_id = p.user_id
                             where f.user_id = @User_id
                             order by p.creation_datetime desc
                             limit 1000";
            var parameters = new NpgsqlParameter[]
            {
                new("User_id", NpgsqlDbType.Uuid) { Value = user_id }
            };
            var data = await npgsql.GetQueryResultAsync(query, parameters, ["user_id", "post", "creation_datetime", "post_id"], TargetSessionAttributes.PreferStandby);
            if (data.Count == 0) return [];
            var posts = new List<Post>();
            foreach (var post in data)
            {
                posts.Add(new Post(post));
            }
            return posts;
        }
    }
}
