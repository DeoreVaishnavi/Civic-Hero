using Amazon.S3;
using Amazon.S3.Model;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net;

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

    public async Task<string> UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var request = new PutObjectRequest
        {
            BucketName = _options.S3BucketName,
            Key = NormalizeKey(objectKey),
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return request.Key;
    }

    public async Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            await _s3Client.GetObjectMetadataAsync(
                _options.S3BucketName,
                NormalizeKey(objectKey),
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        await _s3Client.DeleteObjectAsync(
            _options.S3BucketName,
            NormalizeKey(objectKey),
            cancellationToken);
    }

    public Task<string> GetReadUrlAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.S3BucketName,
            Key = NormalizeKey(objectKey),
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpiryMinutes),
            Verb = HttpVerb.GET
        };

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.S3BucketName))
        {
            throw new InvalidOperationException("AWS:S3BucketName is not configured.");
        }
    }

    private static string NormalizeKey(string objectKey) =>
        objectKey.Replace('\\', '/').TrimStart('/');
}
