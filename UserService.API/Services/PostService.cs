using Libraries.Kafka.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.API.Services
{
    public class PostService(
        IPostRepository postRepo,
        IDistributedCache distributedCache,
        IFriendService friendService)
    {
        public async Task<Guid> AddPostAsync(Guid user_id, string post)
        {
            var postId = Guid.NewGuid();
            var friends = await friendService.GetFriendsAsync(user_id);
            var outboxEntries = friends.Select(f => new OutboxEntry(
                f.ToString(),
                JsonSerializer.Serialize(
                    new FeedUpdateMessage(ActionTypeEnum.Create, postId, user_id, post),
                    Consts.JsonSerializerOptions)
            )).ToArray();
            return await postRepo.AddPostAsync(user_id, post, postId, outboxEntries);
        }

        public async Task UpdatePostAsync(Guid post_id, string post, Guid user_id)
        {
            var friends = await friendService.GetFriendsAsync(user_id);
            var outboxEntries = friends.Select(f => new OutboxEntry(
                f.ToString(),
                JsonSerializer.Serialize(
                    new FeedUpdateMessage(ActionTypeEnum.Update, post_id, user_id, post),
                    Consts.JsonSerializerOptions)
            )).ToArray();
            await postRepo.UpdatePostAsync(post_id, post, user_id, outboxEntries);
        }

        public async Task DeletePostAsync(Guid post_id, Guid user_id)
        {
            var friends = await friendService.GetFriendsAsync(user_id);
            var outboxEntries = friends.Select(f => new OutboxEntry(
                f.ToString(),
                JsonSerializer.Serialize(
                    new FeedUpdateMessage(ActionTypeEnum.Delete, post_id, user_id, null),
                    Consts.JsonSerializerOptions)
            )).ToArray();
            await postRepo.DeletePostAsync(post_id, user_id, outboxEntries);
        }

        public async Task<Post?> GetPostAsync(Guid post_id)
        {
            return await postRepo.GetPostAsync(post_id);
        }

        public async Task<IEnumerable<Post>> GetFeedAsync(Guid user_id, int offset, int limit)
        {
            string key = $"feed-{user_id}";
            List<Post>? cachedFeed = null;
            var cachedFeedJson = await distributedCache.GetStringAsync(key);
            if (cachedFeedJson != null)
            {
                cachedFeed = JsonSerializer.Deserialize<List<Post>>(cachedFeedJson, Consts.JsonSerializerOptions);
            }
            return cachedFeed != null && cachedFeed.Count >= offset + limit
                ? cachedFeed.OrderByDescending(f => f.Creation_datetime).Skip(offset).Take(limit)
                : await postRepo.GetFeedAsync(user_id, offset, limit);
        }
    }
}
