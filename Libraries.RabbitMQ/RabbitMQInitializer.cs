using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Libraries.RabbitMQ;

public class RabbitMQInitializer
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQInitializer> _logger;

    public RabbitMQInitializer(IOptions<RabbitMQSettings> options, ILogger<RabbitMQInitializer> logger)
    {
        _settings = options.Value;
        _logger = logger;
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

                await channel.ExchangeDeclareAsync(
                    exchange: "feed-posts-dlx",
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: ct);

                try
                {
                    await channel.QueueDeclareAsync(
                        queue: "feed-posts",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: new Dictionary<string, object?>
                        {
                            ["x-dead-letter-exchange"] = "feed-posts-dlx",
                            ["x-dead-letter-routing-key"] = "feed-posts-dlx",
                        },
                        cancellationToken: ct);
                }
                catch (Exception)
                {
                    await channel.QueueDeleteAsync("feed-posts", cancellationToken: ct);
                    await channel.QueueDeclareAsync(
                        queue: "feed-posts",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: new Dictionary<string, object?>
                        {
                            ["x-dead-letter-exchange"] = "feed-posts-dlx",
                            ["x-dead-letter-routing-key"] = "feed-posts-dlx",
                        },
                        cancellationToken: ct);
                }

                await channel.QueueBindAsync(
                    queue: "feed-posts",
                    exchange: "feed-posts",
                    routingKey: "feed-posts",
                    cancellationToken: ct);

                await channel.QueueDeclareAsync(
                    queue: "feed-posts-dlx",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: ct);

                await channel.QueueBindAsync(
                    queue: "feed-posts-dlx",
                    exchange: "feed-posts-dlx",
                    routingKey: "feed-posts-dlx",
                    cancellationToken: ct);

                _logger.LogInformation("RabbitMQ topology initialized with DLX");
                return;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "RabbitMQ init attempt {Attempt}/10", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
        _logger.LogError("RabbitMQ init failed after 10 attempts");
        throw new InvalidOperationException("RabbitMQ init failed after 10 retries");
    }
}
