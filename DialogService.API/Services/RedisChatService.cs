using DialogService.API.DTOs;
using DialogService.Database.Entities;
using Libraries.Web.Common.Clients;
using StackExchange.Redis;
using System.Text.Json;

namespace DialogService.API.Services
{
    public class RedisChatService : IChatService, IDisposable, IAsyncDisposable
    {
        private readonly ConnectionMultiplexer redis;
        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly UserServiceClient userService;

        public RedisChatService(IConfiguration configuration, UserServiceClient userService)
        {
#if DEBUG
            var connectionString = configuration.GetConnectionString("redis_debug")!;
#else
            var connectionString = configuration.GetConnectionString("redis")!;
#endif
            redis = ConnectionMultiplexer.Connect(connectionString);
            this.userService = userService;
        }

        public async Task<Guid> CreateChatAsync(CreateChatRequest request, Guid creator_id)
        {
            var db = redis.GetDatabase(0);
            if (!request.Users_ids.Contains(creator_id))
                request.Users_ids.Add(creator_id);

            var chat = new RedisChat
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Users = request.Users_ids,
                Created_at = DateTime.Now
            };
            await db.StringSetAsync($"chat-{chat.Id}", JsonSerializer.Serialize(chat, jsonOptions));

            var chatEntity = new Chat
            {
                Chat_id = chat.Id,
                Chat_name = chat.Name,
                Creation_datetime = chat.Created_at,
                Creator_id = creator_id
            };

            foreach (var user_id in request.Users_ids)
            {
                var remoteUser = await userService.GetUserAsync(user_id);
                if (remoteUser is null) continue;

                var user_chats_res = await db.StringGetAsync($"user_chats-{user_id}");

                if (user_chats_res.IsNullOrEmpty)
                {
                    var new_user_chats = new RedisUserChats
                    {
                        User_id = user_id,
                        Chats = [chatEntity]
                    };
                    await db.StringSetAsync($"user_chats-{user_id}", JsonSerializer.Serialize(new_user_chats, jsonOptions));
                    continue;
                }
                var user_chats = JsonSerializer.Deserialize<RedisUserChats>((string)user_chats_res!, jsonOptions)!;
                user_chats.Chats.Add(chatEntity);
                await db.StringSetAsync($"user_chats-{user_id}", JsonSerializer.Serialize(user_chats, jsonOptions));
            }
            return chat.Id;
        }

        public async Task<Guid> CreateOrGetPersonalChatAsync(Guid currentUserId, Guid targetUserId)
        {
            var (minId, maxId) = currentUserId.CompareTo(targetUserId) < 0
                ? (currentUserId, targetUserId)
                : (targetUserId, currentUserId);

            var personalKey = $"personal:{minId}:{maxId}";
            var db = redis.GetDatabase(0);
            var existing = await db.StringGetAsync(personalKey);

            if (!existing.IsNullOrEmpty && Guid.TryParse((string)existing!, out var existingChatId))
                return existingChatId;

            // Check privacy setting of target user
            var targetUser = await userService.GetUserAsync(targetUserId);
            if (targetUser is null)
                throw new KeyNotFoundException("Target user not found");

            if (targetUser.Who_can_message == 1)
            {
                var isSubscribed = await userService.GetSubscriptionStatusAsync(friendId: targetUserId);
                if (!isSubscribed)
                    throw new UnauthorizedAccessException("User allows messages only from subscribers");
            }

            var chatId = Guid.NewGuid();
            var chat = new RedisChat
            {
                Id = chatId,
                Name = $"{targetUser.First_name} {targetUser.Second_name}",
                Users = [currentUserId, targetUserId],
                Created_at = DateTime.Now
            };
            await db.StringSetAsync($"chat-{chatId}", JsonSerializer.Serialize(chat, jsonOptions));
            await db.StringSetAsync(personalKey, chatId.ToString());

            var chatEntity = new Chat
            {
                Chat_id = chatId,
                Chat_name = chat.Name,
                Creation_datetime = chat.Created_at,
                Creator_id = currentUserId
            };

            foreach (var uid in new[] { currentUserId, targetUserId })
            {
                var user_chats_res = await db.StringGetAsync($"user_chats-{uid}");
                if (user_chats_res.IsNullOrEmpty)
                {
                    await db.StringSetAsync($"user_chats-{uid}", JsonSerializer.Serialize(
                        new RedisUserChats { User_id = uid, Chats = [chatEntity] }, jsonOptions));
                }
                else
                {
                    var user_chats = JsonSerializer.Deserialize<RedisUserChats>((string)user_chats_res!, jsonOptions)!;
                    user_chats.Chats.Add(chatEntity);
                    await db.StringSetAsync($"user_chats-{uid}", JsonSerializer.Serialize(user_chats, jsonOptions));
                }
            }

            return chatId;
        }

        public async Task<MessageDto[]> GetChatAsync(Guid chat_id, int limit, int offset, Guid user_id)
        {
            var db = redis.GetDatabase(0);
            var chat_res = await db.StringGetAsync($"chat-{chat_id}");
            if (chat_res.IsNullOrEmpty) return [];
            var chat = JsonSerializer.Deserialize<RedisChat>(chat_res.ToString(), jsonOptions);
            if (chat is null) return [];
            return chat.Messages.OrderBy(m => m.Created_at).Skip(offset).Take(limit)
                .Select(m => new MessageDto 
                { 
                    Chat_id = chat_id,
                    Creation_datetime = m.Created_at,
                    Message = m.Text,
                    User_id = m.User_id,
                    User_name = m.User_name,
                    Message_id = m.Message_id
                }).ToArray();
        }

        public async Task<List<Chat>> GetUserChatListAsync(Guid user_id, int limit, int offset)
        {
            var db = redis.GetDatabase(0);
            var user_chats_res = await db.StringGetAsync($"user_chats-{user_id}");
            if (user_chats_res.IsNullOrEmpty) return [];
            var user_chats = JsonSerializer.Deserialize<RedisUserChats>(user_chats_res.ToString(), jsonOptions);
            if (user_chats is null) return [];
            return user_chats.Chats.OrderByDescending(m => m.Creation_datetime).Skip(offset).Take(limit).ToList();
        }

        public async Task<Guid> SendMessageToChatAsync(Guid chat_id, SendMessageRequest request, Guid creator_id)
        {
            var db = redis.GetDatabase(0);
            var chat_res = await db.StringGetAsync($"chat-{chat_id}");
            if (chat_res.IsNullOrEmpty)
                throw new KeyNotFoundException("Chat not found");
            var chat = JsonSerializer.Deserialize<RedisChat>(chat_res.ToString(), jsonOptions)!;
            if (!chat.Users.Contains(creator_id))
                throw new UnauthorizedAccessException("User is not a member of this chat");
            var senderUser = await userService.GetUserAsync(creator_id);
            var senderName = senderUser?.First_name ?? "Unknown";
            var msg = new RedisChatMessage
            {
                Created_at = DateTime.Now,
                Message_id = Guid.NewGuid(),
                Text = request.Message,
                User_id = creator_id,
                User_name = senderName
            };
            chat.Messages.Add(msg);
            await db.StringSetAsync($"chat-{chat_id}", JsonSerializer.Serialize(chat, jsonOptions));
            return msg.Message_id;
        }

        public void Dispose() { redis.Dispose(); GC.SuppressFinalize(this); }
        public async ValueTask DisposeAsync() { await redis.DisposeAsync(); GC.SuppressFinalize(this); }
    }
}
