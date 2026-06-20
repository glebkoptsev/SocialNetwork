using DialogService.API.DTOs;
using DialogService.Database;
using DialogService.Database.Entities;
using Libraries.Web.Common.Caching;
using Libraries.Web.Common.Clients;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace DialogService.API.Services
{
    public class DatabaseChatService(
        DialogDbContext writeDb,
        DialogReadDbContext readDb,
        IConnectionMultiplexer redis,
        IDistributedLock distributedLock,
        UserServiceClient userService) : IChatService
    {
        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public async Task<Guid> CreateChatAsync(CreateChatRequest request, Guid creator_id)
        {
            if (!request.Users_ids.Contains(creator_id))
                request.Users_ids.Add(creator_id);

            var chatId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var chat = new Chat
            {
                Chat_id = chatId,
                Chat_name = request.Name,
                Creator_id = creator_id,
                Creation_datetime = now,
                Last_update_datetime = now
            };
            writeDb.Chats.Add(chat);
            foreach (var uid in request.Users_ids)
                writeDb.ChatUsers.Add(new ChatUser { Chat_id = chatId, User_id = uid, Creation_datetime = now });
            await writeDb.SaveChangesAsync();

            await CacheChatAsync(chatId, chat, request.Users_ids, [], now);
            return chatId;
        }

        public async Task<Guid> CreateOrGetPersonalChatAsync(Guid currentUserId, Guid targetUserId)
        {
            var (minId, maxId) = currentUserId.CompareTo(targetUserId) < 0
                ? (currentUserId, targetUserId)
                : (targetUserId, currentUserId);

            var personalKey = $"personal:{minId}:{maxId}";
            var cache = redis.GetDatabase(0);

            var targetUser = await userService.GetUserAsync(targetUserId);
            if (targetUser is null)
                throw new KeyNotFoundException("Target user not found");

            if (targetUser.Who_can_message == 1)
            {
                var isSubscribed = await userService.GetSubscriptionStatusAsync(friendId: targetUserId);
                if (!isSubscribed)
                    throw new UnauthorizedAccessException("User allows messages only from subscribers");
            }

            var existing = await cache.StringGetAsync(personalKey);
            if (!existing.IsNullOrEmpty && Guid.TryParse((string)existing!, out var existingChatId))
                return existingChatId;

            await using var lockHandle = await distributedLock.AcquireAsync(
                $"lock:{personalKey}", TimeSpan.FromSeconds(10));
            if (lockHandle is null)
                throw new InvalidOperationException("Chat creation in progress, try again");

            existing = await cache.StringGetAsync(personalKey);
            if (!existing.IsNullOrEmpty && Guid.TryParse((string)existing!, out existingChatId))
                return existingChatId;

            // Check PostgreSQL for existing personal chat
            var both = await readDb.ChatUsers.Where(cu => cu.User_id == minId).Select(cu => cu.Chat_id)
                .Intersect(readDb.ChatUsers.Where(cu => cu.User_id == maxId).Select(cu => cu.Chat_id))
                .ToListAsync();
            foreach (var cid in both)
            {
                var userCount = await readDb.ChatUsers.CountAsync(cu => cu.Chat_id == cid);
                if (userCount == 2)
                {
                    await cache.StringSetAsync(personalKey, cid.ToString());
                    return cid;
                }
            }

            var chatId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var chat = new Chat
            {
                Chat_id = chatId,
                Chat_name = $"{targetUser.First_name} {targetUser.Second_name}",
                Creator_id = currentUserId,
                Creation_datetime = now,
                Last_update_datetime = now
            };
            writeDb.Chats.Add(chat);
            writeDb.ChatUsers.Add(new ChatUser { Chat_id = chatId, User_id = currentUserId, Creation_datetime = now });
            writeDb.ChatUsers.Add(new ChatUser { Chat_id = chatId, User_id = targetUserId, Creation_datetime = now });
            await writeDb.SaveChangesAsync();

            await cache.StringSetAsync(personalKey, chatId.ToString());
            await CacheChatAsync(chatId, chat, [currentUserId, targetUserId], [], now);
            return chatId;
        }

        public async Task<MessageDto[]> GetChatAsync(Guid chat_id, int limit, int offset, Guid user_id)
        {
            var cacheKey = $"chat-{chat_id}";
            var cache = redis.GetDatabase(0);

            var cached = await cache.StringGetAsync(cacheKey);
            RedisChat? chat = null;
            if (!cached.IsNullOrEmpty)
                chat = JsonSerializer.Deserialize<RedisChat>(cached.ToString(), jsonOptions);

            if (chat is null)
            {
                var chatEntity = await readDb.Chats.FirstOrDefaultAsync(c => c.Chat_id == chat_id);
                if (chatEntity is null) return [];

                var userIds = await readDb.ChatUsers.Where(cu => cu.Chat_id == chat_id).Select(cu => cu.User_id).ToListAsync();
                if (!userIds.Contains(user_id))
                    throw new UnauthorizedAccessException("User is not a member of this chat");

                var messages = await readDb.Messages.Where(m => m.Chat_id == chat_id)
                    .OrderBy(m => m.Creation_datetime).ToListAsync();

                chat = new RedisChat
                {
                    Id = chat_id,
                    Name = chatEntity.Chat_name,
                    Users = userIds,
                    Messages = messages.Select(m => new RedisChatMessage
                    {
                        Message_id = m.Message_id,
                        Text = m.Message,
                        User_id = m.User_id,
                        User_name = m.User_name,
                        Created_at = m.Creation_datetime,
                        Status = m.Status
                    }).ToList(),
                    Created_at = chatEntity.Creation_datetime
                };

                await cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(chat, jsonOptions), CacheTtl);
            }
            else if (!chat.Users.Contains(user_id))
            {
                throw new UnauthorizedAccessException("User is not a member of this chat");
            }

            // Update status: other users' Sent(0) messages → Delivered(1)
            var changed = false;
            foreach (var m in chat.Messages.Where(m => m.User_id != user_id && m.Status < 1))
            {
                changed = true;
                m.Status = 1;
            }
            if (changed)
            {
                await writeDb.Messages.Where(m => m.Chat_id == chat_id && m.User_id != user_id && m.Status < 1)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, 1));
                await cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(chat, jsonOptions), CacheTtl);
            }

            return chat.Messages.OrderBy(m => m.Created_at).Skip(offset).Take(limit)
                .Select(m => new MessageDto
                {
                    Chat_id = chat_id,
                    Creation_datetime = m.Created_at,
                    Message = m.Text,
                    User_id = m.User_id,
                    User_name = m.User_name,
                    Message_id = m.Message_id,
                    Status = m.Status
                }).ToArray();
        }

        public async Task<List<ChatDto>> GetUserChatListAsync(Guid user_id, int limit, int offset)
        {
            var cacheKey = $"user_chats-{user_id}";
            var cache = redis.GetDatabase(0);

            var cached = await cache.StringGetAsync(cacheKey);
            RedisUserChats? userChats = null;
            if (!cached.IsNullOrEmpty)
                userChats = JsonSerializer.Deserialize<RedisUserChats>(cached.ToString(), jsonOptions);

            if (userChats is null)
            {
                var chatIds = await readDb.ChatUsers.Where(cu => cu.User_id == user_id).Select(cu => cu.Chat_id).ToListAsync();
                var chats = await readDb.Chats.Where(c => chatIds.Contains(c.Chat_id))
                    .OrderByDescending(c => c.Creation_datetime).ToListAsync();

                userChats = new RedisUserChats { User_id = user_id, Chats = chats };
                await cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(userChats, jsonOptions), CacheTtl);
            }

            return userChats.Chats.OrderByDescending(m => m.Creation_datetime).Skip(offset).Take(limit)
                .Select(c => new ChatDto
                {
                    Chat_id = c.Chat_id,
                    Creator_id = c.Creator_id,
                    Chat_name = c.Chat_name,
                    Creation_datetime = c.Creation_datetime
                }).ToList();
        }

        public async Task<Guid> SendMessageToChatAsync(Guid chat_id, SendMessageRequest request, Guid creator_id)
        {
            var msgId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var senderUser = await userService.GetUserAsync(creator_id);
            var senderName = senderUser?.First_name ?? "Unknown";

            var isMember = await readDb.ChatUsers.AnyAsync(cu => cu.Chat_id == chat_id && cu.User_id == creator_id);
            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of this chat");

            var msg = new MessageEntity
            {
                Message_id = msgId,
                Chat_id = chat_id,
                User_id = creator_id,
                Message = request.Message,
                User_name = senderName,
                Creation_datetime = now,
                Status = 0
            };
            writeDb.Messages.Add(msg);
            await writeDb.SaveChangesAsync();

            // Update Redis cache
            var cache = redis.GetDatabase(0);
            var cached = await cache.StringGetAsync($"chat-{chat_id}");
            if (!cached.IsNullOrEmpty)
            {
                var chat = JsonSerializer.Deserialize<RedisChat>(cached.ToString(), jsonOptions);
                if (chat is not null)
                {
                    chat.Messages.Add(new RedisChatMessage
                    {
                        Message_id = msgId,
                        Text = request.Message,
                        User_id = creator_id,
                        User_name = senderName,
                        Created_at = now,
                        Status = 0
                    });
                    await cache.StringSetAsync($"chat-{chat_id}", JsonSerializer.Serialize(chat, jsonOptions), CacheTtl);
                }
            }

            return msgId;
        }

        public async Task MarkChatAsReadAsync(Guid chat_id, Guid user_id)
        {
            var isMember = await readDb.ChatUsers.AnyAsync(cu => cu.Chat_id == chat_id && cu.User_id == user_id);
            if (!isMember)
                throw new UnauthorizedAccessException("User is not a member of this chat");

            await writeDb.Messages.Where(m => m.Chat_id == chat_id && m.User_id != user_id && m.Status < 2)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, 2));

            // Update Redis cache
            var cache = redis.GetDatabase(0);
            var cached = await cache.StringGetAsync($"chat-{chat_id}");
            if (!cached.IsNullOrEmpty)
            {
                var chat = JsonSerializer.Deserialize<RedisChat>(cached.ToString(), jsonOptions);
                if (chat is not null)
                {
                    foreach (var m in chat.Messages.Where(m => m.User_id != user_id && m.Status < 2))
                        m.Status = 2;
                    await cache.StringSetAsync($"chat-{chat_id}", JsonSerializer.Serialize(chat, jsonOptions), CacheTtl);
                }
            }
        }

        private async Task CacheChatAsync(Guid chatId, Chat chat, List<Guid> userIds, List<RedisChatMessage> messages, DateTime createdAt)
        {
            var redisChat = new RedisChat
            {
                Id = chatId,
                Name = chat.Chat_name,
                Users = userIds,
                Messages = messages,
                Created_at = createdAt
            };
            var cache = redis.GetDatabase(0);
            await cache.StringSetAsync($"chat-{chatId}", JsonSerializer.Serialize(redisChat, jsonOptions), CacheTtl);
        }
    }
}
