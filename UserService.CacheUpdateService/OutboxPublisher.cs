using Confluent.Kafka;
using Libraries.Kafka;
using System.Text.Json;

namespace UserService.CacheUpdateService
{
    public class OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IKafkaProducer kafkaProducer) : BackgroundService
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
                        var message = new Message<string, string>
                        {
                            Key = record.KafkaKey,
                            Value = record.KafkaValue,
                            Timestamp = Timestamp.Default
                        };
                        await kafkaProducer.ProduceAsync("feed-posts", message);
                        await outboxStore.MarkProcessedAsync(record.Id, ct);
                    }
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
