using Libraries.Web.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.API.Services
{
    public class FriendService(UserDbContext writeDb, UserReadDbContext readDb) : IFriendService
    {
        public async Task AddFriendAsync(Guid user_id, Guid friend_id)
        {
            var exists = await readDb.Friends.AnyAsync(f => f.User_id == user_id && f.Friend_id == friend_id);
            if (!exists)
            {
                writeDb.Friends.Add(new FriendEntity { User_id = user_id, Friend_id = friend_id });
            }

            var outboxValue = JsonSerializer.Serialize(
                new FeedUpdateMessage(ActionTypeEnum.FullReload, null, user_id, null),
                Consts.JsonSerializerOptions);

            writeDb.FeedOutbox.Add(new FeedOutboxEntity
            {
                Kafka_key = user_id.ToString(),
                Kafka_value = outboxValue,
                Created_at = DateTime.UtcNow
            });

            await writeDb.SaveChangesAsync();
        }

        public async Task DeleteFriendAsync(Guid user_id, Guid friend_id)
        {
            var friend = await writeDb.Friends
                .FirstOrDefaultAsync(f => f.User_id == user_id && f.Friend_id == friend_id);
            if (friend is not null)
                writeDb.Friends.Remove(friend);

            var outboxValue = JsonSerializer.Serialize(
                new FeedUpdateMessage(ActionTypeEnum.FullReload, null, user_id, null),
                Consts.JsonSerializerOptions);

            writeDb.FeedOutbox.Add(new FeedOutboxEntity
            {
                Kafka_key = user_id.ToString(),
                Kafka_value = outboxValue,
                Created_at = DateTime.UtcNow
            });

            await writeDb.SaveChangesAsync();
        }

        public async Task<bool> IsFriendAsync(Guid user_id, Guid friend_id)
        {
            return await writeDb.Friends.AnyAsync(f => f.User_id == user_id && f.Friend_id == friend_id);
        }

        public async Task<List<Guid>> GetFollowerIdsAsync(Guid user_id)
        {
            return await readDb.Friends
                .Where(f => f.Friend_id == user_id)
                .Select(f => f.User_id)
                .ToListAsync();
        }
    }
}
