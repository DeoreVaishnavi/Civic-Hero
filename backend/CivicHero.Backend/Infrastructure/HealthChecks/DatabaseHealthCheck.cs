using CivicHero.Backend.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

/// <summary>
/// Checks database connectivity.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly CivicDbContext _dbContext;

    public DatabaseHealthCheck(CivicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect =
                await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return HealthCheckResult.Healthy(
                    "Database connection is healthy.");
            }

            return HealthCheckResult.Unhealthy(
                "Unable to connect to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database health check failed.",
                ex);
        }
    }
}