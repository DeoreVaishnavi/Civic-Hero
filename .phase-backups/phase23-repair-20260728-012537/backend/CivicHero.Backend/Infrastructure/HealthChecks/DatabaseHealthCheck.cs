using CivicHero.Backend.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly CivicDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public DatabaseHealthCheck(CivicDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Degraded(
                "AWS RDS MySQL connection string is not configured.",
                data: new Dictionary<string, object>
                {
                    ["provider"] = "AWS RDS MySQL 8",
                    ["configured"] = false
                });
        }

        try
        {
            var connected = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return connected
                ? HealthCheckResult.Healthy(
                    "AWS RDS MySQL connection is healthy.",
                    new Dictionary<string, object>
                    {
                        ["provider"] = "AWS RDS MySQL 8",
                        ["configured"] = true
                    })
                : HealthCheckResult.Unhealthy("AWS RDS MySQL rejected the connection.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "AWS RDS MySQL connection failed.",
                exception,
                new Dictionary<string, object>
                {
                    ["provider"] = "AWS RDS MySQL 8",
                    ["configured"] = true
                });
        }
    }
}

