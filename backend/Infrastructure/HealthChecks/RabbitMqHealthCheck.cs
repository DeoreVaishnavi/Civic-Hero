
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;
    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options) => _options = options.Value;
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ is disabled."));
        try
        {
            using var connection = RabbitMqPublisher.CreateFactory(_options).CreateConnection();
            using var channel = connection.CreateModel();
            RabbitMqTopology.Declare(channel, _options);
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection and topology are ready."));
        }
        catch (Exception exception) { return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception)); }
    }
}
