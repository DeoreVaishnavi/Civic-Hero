
using System.Text.Json;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, IOptions<RedisOptions> options, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(Normalize(key), cancellationToken);
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cache read failed for {CacheKey}; continuing without cached data.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(Math.Clamp(_options.DefaultTtlMinutes, 1, 1440))
            };
            await _cache.SetStringAsync(Normalize(key), JsonSerializer.Serialize(value, JsonOptions), options, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cache write failed for {CacheKey}; request will continue.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try { await _cache.RemoveAsync(Normalize(key), cancellationToken); }
        catch (Exception exception) { _logger.LogWarning(exception, "Cache removal failed for {CacheKey}.", key); }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null) return cached;
        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);
        return value;
    }

    private string Normalize(string key) => $"{_options.InstanceName}{key.Trim()}";
}
