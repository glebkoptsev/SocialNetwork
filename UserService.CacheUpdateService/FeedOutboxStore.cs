using Microsoft.EntityFrameworkCore;
using UserService.Database;

namespace UserService.CacheUpdateService
{
    public class FeedOutboxStore(UserDbContext context) : IFeedOutboxStore
    {
        private DateTime? lastPurge;

        public async Task<IReadOnlyList<OutboxRecord>> GetUnprocessedAsync(int batchSize, CancellationToken ct)
        {
            var records = await context.FeedOutbox
                .Where(o => o.Processed_at == null)
                .OrderBy(o => o.Id)
                .Take(batchSize)
                .Select(o => new OutboxRecord(o.Id, o.Kafka_key, o.Kafka_value))
                .ToListAsync(ct);
            return records;
        }

        public async Task MarkProcessedAsync(long id, CancellationToken ct)
        {
            await context.FeedOutbox
                .Where(o => o.Id == id)
                .ExecuteUpdateAsync(o => o.SetProperty(x => x.Processed_at, DateTime.UtcNow), ct);
        }

        public async Task PurgeProcessedAsync(CancellationToken ct)
        {
            if (lastPurge is not null && DateTime.UtcNow - lastPurge < TimeSpan.FromHours(1))
                return;
            lastPurge = DateTime.UtcNow;

            var cutoff = DateTime.UtcNow.AddDays(-7);
            await context.FeedOutbox
                .Where(o => o.Processed_at != null && o.Processed_at < cutoff)
                .ExecuteDeleteAsync(ct);
        }
    }
}
