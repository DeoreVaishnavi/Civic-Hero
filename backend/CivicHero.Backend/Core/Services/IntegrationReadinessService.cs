
using CivicHero.Backend.Infrastructure.Caching;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CivicHero.Backend.Core.Services;

public sealed class IntegrationReadinessService : IIntegrationReadinessService
{
    private readonly ICacheService _cache;
    private readonly IMessagePublisher _publisher;
    private readonly RedisOptions _redis;
    private readonly RabbitMqOptions _rabbit;
    private readonly AutomationOptions _automation;
    public IntegrationReadinessService(ICacheService cache, IMessagePublisher publisher, IOptions<RedisOptions> redis, IOptions<RabbitMqOptions> rabbit, IOptions<AutomationOptions> automation)
    { _cache = cache; _publisher = publisher; _redis = redis.Value; _rabbit = rabbit.Value; _automation = automation.Value; }

    public async Task<IntegrationReadinessResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var dependencies = new List<IntegrationDependencyStatus>();
        try { var cacheTest = await TestCacheAsync(cancellationToken); dependencies.Add(new("Redis cache", _redis.Enabled ? "Ready" : "Fallback", _redis.Enabled ? "Distributed cache read/write succeeded." : "Redis disabled; in-memory fallback is active.", false)); }
        catch (Exception exception) { dependencies.Add(new("Redis cache", "Unavailable", exception.Message, false)); }
        if (!_rabbit.Enabled) dependencies.Add(new("RabbitMQ", "Disabled", "Events are skipped until RabbitMQ is enabled.", false));
        else
        {
            try { using var connection = RabbitMqPublisher.CreateFactory(_rabbit).CreateConnection(); dependencies.Add(new("RabbitMQ", connection.IsOpen ? "Ready" : "Unavailable", $"{_rabbit.HostName}:{_rabbit.Port}/{_rabbit.VirtualHost}", false)); }
            catch (Exception exception) { dependencies.Add(new("RabbitMQ", "Unavailable", exception.Message, false)); }
        }
        var overall = dependencies.Any(x => x.Required && x.Status == "Unavailable") ? "Blocked" : dependencies.Any(x => x.Status == "Unavailable") ? "Degraded" : "Ready";
        return new(overall, DateTimeOffset.UtcNow, dependencies, _automation.Enabled ? ["SLA monitor", "Escalation worker", "Verification auto-close", "RabbitMQ consumer"] : ["Automation disabled"], true);
    }

    public async Task<object> TestCacheAsync(CancellationToken cancellationToken = default)
    {
        var key = $"phase17:test:{Guid.NewGuid():N}"; var expected = Guid.NewGuid().ToString("N");
        await _cache.SetAsync(key, expected, TimeSpan.FromMinutes(1), cancellationToken);
        var actual = await _cache.GetAsync<string>(key, cancellationToken);
        await _cache.RemoveAsync(key, cancellationToken);
        if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException("Cache round-trip did not return the expected value.");
        return new { success = true, provider = _redis.Enabled ? "Redis" : "Memory fallback", checkedAtUtc = DateTimeOffset.UtcNow };
    }

    public async Task<object> TestMessagingAsync(string? correlationId, CancellationToken cancellationToken = default)
    {
        if (!_rabbit.Enabled) return new { success = false, skipped = true, message = "RabbitMQ is disabled." };
        var id = Guid.NewGuid(); await _publisher.PublishAsync("civichero.integration-test", new { id, source = "Phase17 Integration Centre" }, correlationId, cancellationToken);
        return new { success = true, eventId = id, exchange = _rabbit.ExchangeName, checkedAtUtc = DateTimeOffset.UtcNow };
    }
}
