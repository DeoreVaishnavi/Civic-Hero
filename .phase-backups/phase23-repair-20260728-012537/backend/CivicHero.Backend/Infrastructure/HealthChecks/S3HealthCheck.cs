using Amazon.S3;
using Amazon.S3.Model;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.HealthChecks;

public sealed class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsOptions _options;

    public S3HealthCheck(IAmazonS3 s3Client, IOptions<AwsOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.S3BucketName))
        {
            return HealthCheckResult.Degraded(
                "Amazon S3 bucket is not configured.",
                data: new Dictionary<string, object>
                {
                    ["provider"] = "Amazon S3",
                    ["configured"] = false,
                    ["region"] = _options.Region
                });
        }

        try
        {
            await _s3Client.GetBucketLocationAsync(
                new GetBucketLocationRequest { BucketName = _options.S3BucketName },
                cancellationToken);

            return HealthCheckResult.Healthy(
                "Amazon S3 bucket is reachable.",
                new Dictionary<string, object>
                {
                    ["provider"] = "Amazon S3",
                    ["configured"] = true,
                    ["region"] = _options.Region,
                    ["bucket"] = _options.S3BucketName
                });
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Amazon S3 health check failed.",
                exception,
                new Dictionary<string, object>
                {
                    ["provider"] = "Amazon S3",
                    ["configured"] = true,
                    ["region"] = _options.Region,
                    ["bucket"] = _options.S3BucketName
                });
        }
    }
}
