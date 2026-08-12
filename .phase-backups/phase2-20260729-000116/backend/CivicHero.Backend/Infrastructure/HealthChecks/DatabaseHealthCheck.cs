using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CivicDbContext>();
            var connected = await dbContext.Database.CanConnectAsync(cancellationToken);

            return connected
                ? HealthCheckResult.Healthy("AWS RDS MySQL connection succeeded.")
                : HealthCheckResult.Unhealthy("AWS RDS MySQL connection failed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "AWS RDS MySQL is unavailable or not configured.",
                exception);
        }
    }
}
