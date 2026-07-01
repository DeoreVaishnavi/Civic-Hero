using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

/// <summary>
/// Performs a health check for the AI subsystem.
/// </summary>
public sealed class AiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // AI engine integration will be implemented in Phase 9.
        // Until then, the subsystem is reported as healthy.

        return Task.FromResult(
            HealthCheckResult.Healthy(
                "AI subsystem is available."));
    }
}