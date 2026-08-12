using System.Net;
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
        if (!_options.HasBucketConfiguration) return false;
        await _s3Client.HeadBucketAsync(new HeadBucketRequest { BucketName = _options.BucketName }, cancellationToken);
        return true;
    }

    public async Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false
        };

        if (metadata is not null)
        {
            foreach (var item in metadata)
                request.Metadata[item.Key] = item.Value;
        }

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<StorageDownload?> DownloadAsync(
        string objectKey,
        string downloadFileName,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        try
        {
            using var response = await _s3Client.GetObjectAsync(_options.BucketName, objectKey, cancellationToken);
            var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            return new StorageDownload(
                memory,
                string.IsNullOrWhiteSpace(response.Headers.ContentType) ? "application/octet-stream" : response.Headers.ContentType,
                downloadFileName);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await _s3Client.DeleteObjectAsync(_options.BucketName, objectKey, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (!_options.HasBucketConfiguration)
            throw new InvalidOperationException("AWS S3 bucket configuration is missing.");
    }
}
