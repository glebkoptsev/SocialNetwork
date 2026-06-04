using Libraries.NpgsqlService;
using Npgsql;
using NpgsqlTypes;

namespace UserService.CacheUpdateService
{
    public class FeedOutboxStore(INpgsqlService npgsqlService) : IFeedOutboxStore
    {
        public async Task<IReadOnlyList<OutboxRecord>> GetUnprocessedAsync(int batchSize, CancellationToken ct)
        {
            string query = @"SELECT id, kafka_key, kafka_value
                             FROM public.feed_outbox
                             WHERE processed_at IS NULL
                             ORDER BY id ASC
                             LIMIT @Limit";
            var parameters = new NpgsqlParameter[]
            {
                new("Limit", NpgsqlDbType.Integer) { Value = batchSize }
            };
            var data = await npgsqlService.GetQueryResultAsync(query, parameters,
                ["id", "kafka_key", "kafka_value"], TargetSessionAttributes.PreferStandby);

            return data.Select(row => new OutboxRecord(
                Id: Convert.ToInt64(row["id"]),
                KafkaKey: row["kafka_key"].ToString()!,
                KafkaValue: row["kafka_value"].ToString()!
            )).ToList();
        }

        public async Task MarkProcessedAsync(long id, CancellationToken ct)
        {
            string query = @"UPDATE public.feed_outbox
                             SET processed_at = NOW()
                             WHERE id = @Id";
            var parameters = new NpgsqlParameter[]
            {
                new("Id", NpgsqlDbType.Bigint) { Value = id }
            };
            await npgsqlService.ExecuteNonQueryAsync(query, parameters);
        }
    }
}
