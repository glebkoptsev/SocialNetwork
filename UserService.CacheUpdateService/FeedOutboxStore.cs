using Microsoft.EntityFrameworkCore;
using UserService.Database;

namespace UserService.CacheUpdateService
{
    public class FeedOutboxStore(UserDbContext context) : IFeedOutboxStore
    {
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
            var record = await context.FeedOutbox.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (record is not null)
            {
                record.Processed_at = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);
            }
        }
    }
}
