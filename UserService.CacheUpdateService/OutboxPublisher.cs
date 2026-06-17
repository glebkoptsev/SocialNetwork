using Libraries.RabbitMQ;
using Microsoft.EntityFrameworkCore;

namespace UserService.CacheUpdateService
{
    public class OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IRabbitMQPublisher rabbitMQPublisher) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var outboxStore = scope.ServiceProvider.GetRequiredService<IFeedOutboxStore>();
                    var records = await outboxStore.GetUnprocessedAsync(100, ct);
                    if (records.Count == 0)
                    {
                        await Task.Delay(1000, ct);
                        continue;
                    }

                    foreach (var record in records)
                    {
                        await rabbitMQPublisher.PublishAsync("feed-posts", "feed-posts", record.KafkaValue);
                        await outboxStore.MarkProcessedAsync(record.Id, ct);
                    }

                    // Purge old processed records periodically
                    await outboxStore.PurgeProcessedAsync(ct);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Console.WriteLine(e.ToString());
                    await Task.Delay(5000, ct);
                }
            }
        }
    }
}
