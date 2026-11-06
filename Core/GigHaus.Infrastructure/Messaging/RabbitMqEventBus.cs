using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace GigHaus.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of <see cref="IEventBus"/>.
/// Uses RabbitMQ.Client 7.x async API.
/// </summary>
public class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly IConnection _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    public const string ExchangeName = "gighaus.events";

    public RabbitMqEventBus(IConnection connection)
    {
        _connection = connection;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is not null)
            return _channel;

        await _channelLock.WaitAsync(ct);
        try
        {
            if (_channel is not null)
                return _channel;

            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);
            return _channel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken ct = default)
        where T : class
    {
        var channel = await GetChannelAsync(ct);
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = typeof(T).Name
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
    }
}

