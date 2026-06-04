using Libraries.NpgsqlService;
using Npgsql;
using NpgsqlTypes;
using UserService.Database.Entities;

namespace UserService.Database
{
    public class PostRepository(INpgsqlService npgsqlService) : IPostRepository
    {
        public async Task<Guid> AddPostAsync(Guid user_id, string post, Guid postId, OutboxEntry[]? outboxEntries = null)
        {
            var businessQuery = @"INSERT INTO public.posts (post_id, user_id, post)
                                  VALUES (@Post_id, @User_id, @Post)";
            var businessParams = new NpgsqlParameter[]
            {
               new("Post_id", NpgsqlDbType.Uuid) { Value = postId },
               new("User_id", NpgsqlDbType.Uuid) { Value = user_id },
               new("Post", NpgsqlDbType.Varchar) { Value = post }
            };
            await ExecuteWithOutboxAsync(businessQuery, businessParams, outboxEntries);
            return postId;
        }

        public async Task UpdatePostAsync(Guid post_id, string post, Guid user_id, OutboxEntry[]? outboxEntries = null)
        {
            var businessQuery = @"UPDATE public.posts
                                  SET post = @Post
                                  WHERE post_id = @Post_id and user_id = @User_id";
            var businessParams = new NpgsqlParameter[]
            {
               new("Post_id", NpgsqlDbType.Uuid) { Value = post_id },
               new("User_id", NpgsqlDbType.Uuid) { Value = user_id },
               new("Post", NpgsqlDbType.Varchar) { Value = post }
            };
            await ExecuteWithOutboxAsync(businessQuery, businessParams, outboxEntries);
        }

        public async Task DeletePostAsync(Guid post_id, Guid user_id, OutboxEntry[]? outboxEntries = null)
        {
            var businessQuery = @"DELETE FROM public.posts
                                  WHERE post_id = @Post_id and user_id = @User_id";
            var businessParams = new NpgsqlParameter[]
            {
               new("Post_id", NpgsqlDbType.Uuid) { Value = post_id },
               new("User_id", NpgsqlDbType.Uuid) { Value = user_id }
            };
            await ExecuteWithOutboxAsync(businessQuery, businessParams, outboxEntries);
        }

        private async Task ExecuteWithOutboxAsync(string businessQuery, NpgsqlParameter[] businessParams, OutboxEntry[]? outboxEntries)
        {
            if (outboxEntries is null || outboxEntries.Length == 0)
            {
                await npgsqlService.ExecuteNonQueryAsync(businessQuery, businessParams);
                return;
            }

            var queries = new List<string> { businessQuery };
            var allParams = new List<NpgsqlParameter[]> { businessParams };

            foreach (var entry in outboxEntries)
            {
                queries.Add(@"INSERT INTO public.feed_outbox (kafka_key, kafka_value)
                              VALUES (@Key, @Value)");
                allParams.Add(new NpgsqlParameter[]
                {
                    new("Key", NpgsqlDbType.Varchar) { Value = entry.KafkaKey },
                    new("Value", NpgsqlDbType.Text) { Value = entry.KafkaValue }
                });
            }

            await npgsqlService.ExecuteTransactionAsync(queries.ToArray(), allParams.ToArray());
        }

        public async Task<Post?> GetPostAsync(Guid post_id)
        {
            string query = @"SELECT user_id, post, creation_datetime FROM public.posts
                            WHERE post_id = @Post_id";
            var parameters = new NpgsqlParameter[]
            {
               new("Post_id", NpgsqlDbType.Uuid) { Value = post_id }
            };
            var data = await npgsqlService.GetQueryResultAsync(query, parameters, ["user_id", "post", "creation_datetime"], TargetSessionAttributes.PreferStandby);
            if (data.Count == 0) return null;
            return new Post(post_id, data[0]);
        }

        public async Task<List<Post>> GetFeedAsync(Guid user_id, int offset, int limit)
        {
            string query = @"select p.user_id, p.post_id, p.creation_datetime, p.post 
                            from friends f
                            inner join posts p on f.friend_id = p.user_id
                            where f.user_id = @User_id
                            order by p.creation_datetime desc
                            limit @Limit offset @Offset";
            var parameters = new NpgsqlParameter[]
            {
               new("User_id", NpgsqlDbType.Uuid) { Value = user_id },
               new("Limit", NpgsqlDbType.Integer) { Value = limit },
               new("Offset", NpgsqlDbType.Integer) { Value = offset }
            };
            var data = await npgsqlService.GetQueryResultAsync(query, parameters, ["user_id", "post", "creation_datetime", "post_id"], TargetSessionAttributes.PreferStandby);
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
