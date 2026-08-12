using Amazon.S3;
using Amazon.S3.Model;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Storage;

public sealed class AmazonS3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsOptions _options;

    public AmazonS3StorageService(IAmazonS3 s3Client, IOptions<AwsOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public string Provider => "S3";
    public string BucketName => _options.BucketName;

    public async Task<bool> IsBucketAccessibleAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.HasBucketConfiguration)
        {
            return false;
        }

        var request = new HeadBucketRequest
        {
            BucketName = _options.BucketName
        };

        await _s3Client.HeadBucketAsync(request, cancellationToken);
        return true;
    }
}
