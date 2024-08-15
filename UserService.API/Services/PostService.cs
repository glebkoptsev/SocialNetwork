using Confluent.Kafka;
using Libraries.Kafka;
using Libraries.Kafka.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.API.Services
{
    public class PostService(PostRepository postRepo, IDistributedCache distributedCache, KafkaProducer<string, string> kafkaProducer)
    {
        private readonly PostRepository postRepo = postRepo;
        private readonly KafkaProducer<string, string> kafkaProducer = kafkaProducer;

        public async Task<Guid> AddPostAsync(Guid user_id, string post)
        {
            var post_id = await postRepo.AddPostAsync(user_id, post);
            var message = new Message<string, string>
            {
                Key = user_id.ToString(),
                Value = JsonSerializer.Serialize(new FeedUpdateMessage(ActionTypeEnum.Create, post_id), Consts.JsonSerializerOptions),
                Timestamp = Timestamp.Default
            };
            await kafkaProducer.ProduceAsync("feed-posts", message);
            return post_id;
        }

        public async Task UpdatePostAsync(Guid post_id, string post, Guid user_id)
        {
            await postRepo.UpdatePostAsync(post_id, post, user_id);
            var message = new Message<string, string>
            {
                Key = user_id.ToString(),
                Value = JsonSerializer.Serialize(new FeedUpdateMessage(ActionTypeEnum.Update, post_id), Consts.JsonSerializerOptions),
                Timestamp = Timestamp.Default
            };
            await kafkaProducer.ProduceAsync("feed-posts", message);
        }

        public async Task DeletePostAsync(Guid post_id, Guid user_id)
        {
            await postRepo.DeletePostAsync(post_id, user_id);
            var message = new Message<string, string>
            {
                Key = user_id.ToString(),
                Value = JsonSerializer.Serialize(new FeedUpdateMessage(ActionTypeEnum.Delete, post_id), Consts.JsonSerializerOptions),
                Timestamp = Timestamp.Default
            };
            await kafkaProducer.ProduceAsync("feed-posts", message);
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
            //если кеша нет или в нём нет нужного кол-ва данных, то берем из базы
            return cachedFeed != null && cachedFeed.Count >= offset + limit
                ? cachedFeed.OrderByDescending(f => f.Creation_datetime).Skip(offset).Take(limit)
                : await postRepo.GetFeedAsync(user_id, offset, limit);
        }
    }
}
