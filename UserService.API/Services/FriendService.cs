using Libraries.Kafka.DTOs;
using Libraries.NpgsqlService;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using UserService.Database;

namespace UserService.API.Services
{
    public class FriendService(INpgsqlService npgsqlService) : IFriendService
    {

        public async Task AddFriendAsync(Guid user_id, Guid friend_id)
        {
            string query = @"INSERT INTO public.friends (user_id, friend_id)
                                VALUES (@User_id, @Friend_id)";
            var parameters = new NpgsqlParameter[]
            {
                new("User_id", NpgsqlDbType.Uuid) { Value = user_id },
                new("Friend_id", NpgsqlDbType.Uuid) { Value = friend_id },
            };
            var outboxValue = JsonSerializer.Serialize(
                new FeedUpdateMessage(ActionTypeEnum.FullReload, null, user_id, null),
                Consts.JsonSerializerOptions);
            await npgsqlService.ExecuteTransactionAsync(
                [query, outboxInsert],
                [parameters, outboxParams(user_id.ToString(), outboxValue)]);
        }

        public async Task DeleteFriendAsync(Guid user_id, Guid friend_id)
        {
            string query = @"DELETE FROM public.friends
                             WHERE user_id = @User_id and friend_id = @Friend_id";
            var parameters = new NpgsqlParameter[]
            {
                new("User_id", NpgsqlDbType.Uuid) { Value = user_id },
                new("Friend_id", NpgsqlDbType.Uuid) { Value = friend_id },
            };
            var outboxValue = JsonSerializer.Serialize(
                new FeedUpdateMessage(ActionTypeEnum.FullReload, null, user_id, null),
                Consts.JsonSerializerOptions);
            await npgsqlService.ExecuteTransactionAsync(
                [query, outboxInsert],
                [parameters, outboxParams(user_id.ToString(), outboxValue)]);
        }

        public async Task<List<Guid>> GetFriendsAsync(Guid user_id)
        {
            string query = @"select user_id from public.friends
                             where friend_id = @Friend_id";

            var parameters = new NpgsqlParameter[]
            {
                new("Friend_id", NpgsqlDbType.Uuid) { Value = user_id},
            };
            var data = await npgsqlService.GetQueryResultAsync(query, parameters, ["user_id"], TargetSessionAttributes.PreferStandby);
            if (data.Count == 0) return [];
            var posts = new List<Guid>();
            foreach (var row in data)
            {
                posts.Add(Guid.Parse(row["user_id"].ToString()!));
            }
            return posts;
        }

        private static readonly string outboxInsert =
            @"INSERT INTO public.feed_outbox (kafka_key, kafka_value) VALUES (@Key, @Value)";

        private static NpgsqlParameter[] outboxParams(string key, string value) =>
        [
            new("Key", NpgsqlDbType.Varchar) { Value = key },
            new("Value", NpgsqlDbType.Text) { Value = value }
        ];
    }
}
