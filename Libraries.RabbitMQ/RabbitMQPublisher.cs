using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Libraries.RabbitMQ;

public class RabbitMQPublisher : IRabbitMQPublisher, IAsyncDisposable
{
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMQPublisher(IOptions<RabbitMQSettings> options)
    {
        _settings = options.Value;
    }

    public async Task PublishAsync(string exchange, string routingKey, string message)
    {
        await EnsureConnectionAsync();
        var body = Encoding.UTF8.GetBytes(message);
        var props = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
        await _channel!.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection is { IsOpen: true }) return;

        await _lock.WaitAsync();
        try
        {
            if (_connection is { IsOpen: true }) return;

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
                    var connection = await factory.CreateConnectionAsync();
                    var channel = await connection.CreateChannelAsync();
                    _connection = connection;
                    _channel = channel;
                    Console.WriteLine("RabbitMQ publisher connected");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Попытка {attempt}/10 подключения publisher к RabbitMQ: {e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
            throw new InvalidOperationException("RabbitMQ publisher не смог подключиться после 10 попыток");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _lock.Dispose();
    }
}
