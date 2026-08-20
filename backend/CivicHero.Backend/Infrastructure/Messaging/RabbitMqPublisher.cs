
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CivicHero.Backend.Infrastructure.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync<T>(string eventName, T payload, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) { _logger.LogDebug("RabbitMQ disabled; skipped event {EventName}.", eventName); return Task.CompletedTask; }
        cancellationToken.ThrowIfCancellationRequested();
        var envelope = new IntegrationEventEnvelope(Guid.NewGuid(), eventName, DateTimeOffset.UtcNow, correlationId ?? Guid.NewGuid().ToString("N"), payload!);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));
        var factory = CreateFactory(_options);
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        RabbitMqTopology.Declare(channel, _options);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = envelope.EventId.ToString("N");
        properties.CorrelationId = envelope.CorrelationId;
        properties.Timestamp = new AmqpTimestamp(envelope.OccurredAtUtc.ToUnixTimeSeconds());
        channel.BasicPublish(_options.ExchangeName, eventName, true, properties, body);
        return Task.CompletedTask;
    }

    internal static ConnectionFactory CreateFactory(RabbitMqOptions options) => new()
    {
        HostName = options.HostName,
        Port = options.Port,
        VirtualHost = options.VirtualHost,
        UserName = options.UserName,
        Password = options.Password,
        DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        RequestedHeartbeat = TimeSpan.FromSeconds(30)
    };
}

internal static class RabbitMqTopology
{
    public static void Declare(IModel channel, RabbitMqOptions options)
    {
        var retryExchange = options.ExchangeName + ".retry";
        var deadExchange = options.ExchangeName + ".dead";
        var retryQueue = options.QueueName + ".retry";
        var deadQueue = options.QueueName + ".dead";

        channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(retryExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.ExchangeDeclare(deadExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(options.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.QueueName, options.ExchangeName, "#");
        channel.QueueDeclare(retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            ["x-message-ttl"] = Math.Max(1000, options.RetryDelayMilliseconds),
            ["x-dead-letter-exchange"] = options.ExchangeName,
            ["x-dead-letter-routing-key"] = "retry.return"
        });
        channel.QueueBind(retryQueue, retryExchange, options.QueueName);
        channel.QueueDeclare(deadQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(deadQueue, deadExchange, options.QueueName);
    }
}
