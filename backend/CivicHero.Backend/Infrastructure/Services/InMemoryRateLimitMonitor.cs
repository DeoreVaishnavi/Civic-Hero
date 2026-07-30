using CivicHero.Backend.Core.DTOs.Security;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.Services;

public sealed class InMemoryRateLimitMonitor : IRateLimitMonitor
{
    private readonly DateTimeOffset _startedAtUtc = DateTimeOffset.UtcNow;
    private long _allowed;
    private long _rejected;
    private long _authenticationRejected;
    private long _uploadRejected;
    private long _administrationRejected;

    public void RecordAllowed(string policy) => Interlocked.Increment(ref _allowed);

    public void RecordRejected(string policy)
    {
        Interlocked.Increment(ref _rejected);
        if (policy.Equals("authentication", StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _authenticationRejected);
        else if (policy.Equals("uploads", StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _uploadRejected);
        else if (policy.Equals("administration", StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _administrationRejected);
    }

    public RateLimitMetricsDto Snapshot() => new(
        Interlocked.Read(ref _allowed),
        Interlocked.Read(ref _rejected),
        Interlocked.Read(ref _authenticationRejected),
        Interlocked.Read(ref _uploadRejected),
        Interlocked.Read(ref _administrationRejected),
        _startedAtUtc);
}
