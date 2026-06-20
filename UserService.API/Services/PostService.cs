using Libraries.Web.Common.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using System.Text.Json;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.API.Services
{
    public class PostService(
        IPostRepository postRepo,
        IDistributedCache distributedCache)
    {
        public async Task<Guid> AddPostAsync(Guid user_id, string post)
        {
            var postId = Guid.NewGuid();
            var outboxEntry = new OutboxEntry(
                user_id.ToString(),
                JsonSerializer.Serialize(
                    new FeedUpdateMessage(ActionTypeEnum.Create, postId, user_id, post),
                    Consts.JsonSerializerOptions)
            );
            return await postRepo.AddPostAsync(user_id, post, postId, outboxEntry);
        }

        public async Task UpdatePostAsync(Guid post_id, string post, Guid user_id)
        {
            var outboxEntry = new OutboxEntry(
                user_id.ToString(),
                JsonSerializer.Serialize(
                    new FeedUpdateMessage(ActionTypeEnum.Update, post_id, user_id, post),
                    Consts.JsonSerializerOptions)
            );
            await postRepo.UpdatePostAsync(post_id, post, user_id, outboxEntry);
        }

        public async Task DeletePostAsync(Guid post_id, Guid user_id)
        {
            var outboxEntry = new OutboxEntry(
                user_id.ToString(),
                JsonSerializer.Serialize(
                    new FeedUpdateMessage(ActionTypeEnum.Delete, post_id, user_id, null),
                    Consts.JsonSerializerOptions)
            );
            await postRepo.DeletePostAsync(post_id, user_id, outboxEntry);
        }

        public async Task<Post?> GetPostAsync(Guid post_id)
        {
            try
            {
                return await postRepo.GetPostAsync(post_id);
            }
            catch (NpgsqlException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Post>> GetUserPostsAsync(Guid author_id, int offset, int limit)
        {
            try
            {
                return await postRepo.GetUserPostsAsync(author_id, offset, limit);
            }
            catch (NpgsqlException)
            {
                return [];
            }
        }

        public async Task<IEnumerable<Post>> GetFeedAsync(Guid user_id, int offset, int limit)
        {
            string key = $"feed-{user_id}";
            List<Post>? cachedFeed = null;
            try
            {
                var cachedFeedJson = await distributedCache.GetStringAsync(key);
                if (cachedFeedJson != null)
                    cachedFeed = JsonSerializer.Deserialize<List<Post>>(cachedFeedJson, Consts.JsonSerializerOptions);
            }
            catch
            {
                // Redis недоступен
            }

            if (cachedFeed != null)
            {
                if (cachedFeed.Count >= offset + limit)
                    return cachedFeed.OrderByDescending(f => f.Creation_datetime).Skip(offset).Take(limit);
                // Cache miss for pagination — отдаём что есть, не лезем в PG
                return cachedFeed.OrderByDescending(f => f.Creation_datetime).Skip(offset).Take(cachedFeed.Count - offset > 0 ? limit : 0);
            }

            try
            {
                return await postRepo.GetFeedAsync(user_id, offset, limit);
            }
            catch (NpgsqlException)
            {
                return [];
            }
        }
    }
}
