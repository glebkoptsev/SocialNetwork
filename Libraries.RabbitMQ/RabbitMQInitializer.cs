using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Libraries.RabbitMQ;

public class RabbitMQInitializer
{
    private readonly RabbitMQSettings _settings;

    public RabbitMQInitializer(IOptions<RabbitMQSettings> options)
    {
        _settings = options.Value;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        for (int attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                };
                await using var connection = await factory.CreateConnectionAsync(cancellationToken: ct);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

                await channel.ExchangeDeclareAsync(
                    exchange: "feed-posts",
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: ct);

                await channel.QueueDeclareAsync(
                    queue: "feed-posts",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: ct);

                await channel.QueueBindAsync(
                    queue: "feed-posts",
                    exchange: "feed-posts",
                    routingKey: "feed-posts",
                    cancellationToken: ct);

                Console.WriteLine("RabbitMQ topology initialized");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Попытка {attempt}/10 создать топологию RabbitMQ: {e.Message}");
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
        Console.WriteLine("Не удалось инициализировать RabbitMQ после 10 попыток");
        throw new InvalidOperationException("RabbitMQ init failed after 10 retries");
    }
}
