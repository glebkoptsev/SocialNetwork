using Libraries.NpgsqlService;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using UserService.Database.Entities;

namespace UserService.CacheWarmup
{
    internal class CacheWarmuper(NpgsqlService npgsql, IDistributedCache distributedCache)
    {
        private readonly NpgsqlService npgsql = npgsql;
        private readonly IDistributedCache distributedCache = distributedCache;
        private readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task WarmupAsync()
        {
            await CreateUserDbSchemaAsync();
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

        private async Task CreateUserDbSchemaAsync()
        {
            var query = @"CREATE TABLE IF NOT EXISTS public.users
                          (
                              user_id uuid NOT NULL,
                              first_name character varying(30) NOT NULL,
                              second_name character varying(30) NOT NULL,
                              birthdate character varying(11) NOT NULL,
                              biography character varying(1000) NOT NULL,
                              city character varying(255) NOT NULL,
                              password character varying(255) NOT NULL,
                              can_publish_messages bool null,
                              CONSTRAINT pk_users PRIMARY KEY (user_id)
                          );
                        CREATE INDEX IF NOT EXISTS users_fname_sname_idx ON public.users(first_name varchar_pattern_ops, second_name varchar_pattern_ops);
                        CREATE TABLE IF NOT EXISTS public.friends 
                        (
	                        user_id uuid,
	                        friend_id uuid,
	                        PRIMARY KEY(user_id, friend_id),
	                        FOREIGN KEY (user_id) REFERENCES users (user_id),
	                        FOREIGN KEY (friend_id) REFERENCES users (user_id)
                        );
                        CREATE TABLE IF NOT EXISTS public.posts 
                        (
                            post_id uuid not null,
	                        user_id uuid not null,
	                        post varchar(2000) not null,
                            creation_datetime timestamp not null default CURRENT_TIMESTAMP,
	                        PRIMARY KEY(post_id),
	                        FOREIGN KEY (user_id) REFERENCES users (user_id)
                        );
                        CREATE INDEX IF NOT EXISTS posts_userid_idx ON public.posts(user_id);";


            await npgsql.ExecuteNonQueryAsync(query, []);
        }
    }
}
