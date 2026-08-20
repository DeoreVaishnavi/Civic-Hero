
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly RedisOptions _options;
    public RedisHealthCheck(IDistributedCache cache, IOptions<RedisOptions> options) { _cache = cache; _options = options.Value; }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return HealthCheckResult.Healthy("Redis is disabled; memory-cache fallback is active.");
        var key = $"health:{Guid.NewGuid():N}";
        try
        {
            await _cache.SetStringAsync(key, "ok", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }, cancellationToken);
            var value = await _cache.GetStringAsync(key, cancellationToken);
            await _cache.RemoveAsync(key, cancellationToken);
            return value == "ok" ? HealthCheckResult.Healthy("Redis read/write succeeded.") : HealthCheckResult.Degraded("Redis returned an unexpected value.");
        }
        catch (Exception exception) { return HealthCheckResult.Unhealthy("Redis is unavailable.", exception); }
    }
}
