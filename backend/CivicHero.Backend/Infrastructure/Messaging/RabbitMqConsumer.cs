
using System.Text;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CivicHero.Backend.Infrastructure.Messaging;

public sealed class RabbitMqConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumer(IOptions<RabbitMqOptions> options, IServiceScopeFactory scopeFactory, ILogger<RabbitMqConsumer> logger)
    { _options = options.Value; _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) { _logger.LogInformation("RabbitMQ consumer is disabled."); return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = RabbitMqPublisher.CreateFactory(_options).CreateConnection();
                _channel = _connection.CreateModel();
                RabbitMqTopology.Declare(_channel, _options);
                _channel.BasicQos(0, _options.PrefetchCount, false);
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += HandleAsync;
                _channel.BasicConsume(_options.QueueName, autoAck: false, consumer);
                _logger.LogInformation("RabbitMQ consumer connected to {Host}/{Queue}.", _options.HostName, _options.QueueName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                _logger.LogError(exception, "RabbitMQ consumer failed; reconnecting in 10 seconds.");
                DisposeConnection();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null) return;
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
            db.AuditLogs.Add(new AuditLog
            {
                Action = "IntegrationEventConsumed",
                EntityName = "IntegrationEvent",
                EntityId = args.BasicProperties.MessageId,
                NewValuesJson = json.Length <= 4000 ? json : json[..4000],
                CorrelationId = args.BasicProperties.CorrelationId,
                Severity = "Information",
                Success = true,
                HttpStatusCode = 200,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
            _channel.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "RabbitMQ event processing failed for delivery {DeliveryTag}.", args.DeliveryTag);
            RetryOrDeadLetter(args);
        }
    }

    private void RetryOrDeadLetter(BasicDeliverEventArgs args)
    {
        if (_channel is null) return;
        var headers = args.BasicProperties.Headers is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(args.BasicProperties.Headers);
        var attempts = ReadRetryCount(headers) + 1;
        headers["x-civichero-retry-count"] = attempts;
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = args.BasicProperties.ContentType ?? "application/json";
        properties.MessageId = args.BasicProperties.MessageId;
        properties.CorrelationId = args.BasicProperties.CorrelationId;
        properties.Headers = headers;
        var exchange = attempts <= Math.Max(0, _options.MaximumRetries)
            ? _options.ExchangeName + ".retry"
            : _options.ExchangeName + ".dead";
        _channel.BasicPublish(exchange, _options.QueueName, true, properties, args.Body);
        _channel.BasicAck(args.DeliveryTag, false);
    }

    private static int ReadRetryCount(IDictionary<string, object> headers)
    {
        if (!headers.TryGetValue("x-civichero-retry-count", out var value)) return 0;
        return value switch { int number => number, long number => checked((int)number), byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed, _ => 0 };
    }

    public override void Dispose() { DisposeConnection(); base.Dispose(); }
    private void DisposeConnection() { try { _channel?.Close(); } catch { } try { _connection?.Close(); } catch { } _channel?.Dispose(); _connection?.Dispose(); _channel = null; _connection = null; }
}
