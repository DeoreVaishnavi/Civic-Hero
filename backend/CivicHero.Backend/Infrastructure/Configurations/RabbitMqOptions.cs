
namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public bool Enabled { get; set; }
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "civichero.events";
    public string QueueName { get; set; } = "civichero.backend";
    public ushort PrefetchCount { get; set; } = 20;
    public int MaximumRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 15000;
}
