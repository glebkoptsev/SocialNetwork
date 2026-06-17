namespace UserService.CacheUpdateService
{
    public interface IFeedOutboxStore
    {
        Task<IReadOnlyList<OutboxRecord>> GetUnprocessedAsync(int batchSize, CancellationToken ct);
        Task MarkProcessedAsync(long id, CancellationToken ct);
        Task PurgeProcessedAsync(CancellationToken ct);
    }

    public record OutboxRecord(long Id, string KafkaKey, string KafkaValue);
}
