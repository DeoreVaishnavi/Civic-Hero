using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

public sealed class S3HealthCheck : IHealthCheck
{
    private readonly IStorageService _storageService;

    public S3HealthCheck(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var available = await _storageService.IsBucketAccessibleAsync(cancellationToken);

            return available
                ? HealthCheckResult.Healthy(
                    $"Amazon S3 bucket '{_storageService.BucketName}' is accessible.")
                : HealthCheckResult.Unhealthy("Amazon S3 bucket is not configured.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Amazon S3 is unavailable, the bucket is missing, or access was denied.",
                exception);
        }
    }
}
