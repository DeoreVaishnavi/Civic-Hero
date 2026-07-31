using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Storage;

/// <summary>
/// Development-oriented storage adapter. It prefers S3, but keeps complaint
/// submission functional by falling back to local disk when S3 is unavailable.
/// Production remains S3-only unless this provider is explicitly selected.
/// </summary>
public sealed class ResilientStorageService : IStorageService
{
    private readonly AmazonS3StorageService _primary;
    private readonly LocalFileStorageService _fallback;
    private readonly ILogger<ResilientStorageService> _logger;
    private readonly TimeSpan _primaryAttemptTimeout;
    private bool _primaryUnavailable;

    public ResilientStorageService(
        AmazonS3StorageService primary,
        LocalFileStorageService fallback,
        IOptions<StorageOptions> options,
        ILogger<ResilientStorageService> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
        _primaryAttemptTimeout = TimeSpan.FromSeconds(
            Math.Clamp(options.Value.PrimaryAttemptTimeoutSeconds, 1, 30));
    }

    public string Provider => "S3WithLocalFallback";
    public string BucketName => _primary.BucketName;

    public async Task<bool> IsBucketAccessibleAsync(CancellationToken cancellationToken = default)
    {
        if (!_primaryUnavailable)
        {
            try
            {
                using var timeout = CreatePrimaryTimeout(cancellationToken);
                if (await _primary.IsBucketAccessibleAsync(timeout.Token)) return true;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                MarkPrimaryUnavailable(exception, "health check");
            }
        }

        return await _fallback.IsBucketAccessibleAsync(cancellationToken);
    }

    public async Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await using var buffered = new MemoryStream();
        await stream.CopyToAsync(buffered, cancellationToken);

        if (!_primaryUnavailable)
        {
            try
            {
                buffered.Position = 0;
                using var timeout = CreatePrimaryTimeout(cancellationToken);
                await _primary.UploadAsync(buffered, objectKey, contentType, metadata, timeout.Token);
                return;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                MarkPrimaryUnavailable(exception, $"upload '{objectKey}'");
            }
        }

        buffered.Position = 0;
        await _fallback.UploadAsync(buffered, objectKey, contentType, metadata, cancellationToken);
    }

    public async Task<StorageDownload?> DownloadAsync(
        string objectKey,
        string downloadFileName,
        CancellationToken cancellationToken = default)
    {
        if (!_primaryUnavailable)
        {
            try
            {
                using var timeout = CreatePrimaryTimeout(cancellationToken);
                var primaryResult = await _primary.DownloadAsync(objectKey, downloadFileName, timeout.Token);
                if (primaryResult is not null) return primaryResult;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                MarkPrimaryUnavailable(exception, $"download '{objectKey}'");
            }
        }

        return await _fallback.DownloadAsync(objectKey, downloadFileName, cancellationToken);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        Exception? primaryFailure = null;

        if (!_primaryUnavailable)
        {
            try
            {
                using var timeout = CreatePrimaryTimeout(cancellationToken);
                await _primary.DeleteAsync(objectKey, timeout.Token);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                primaryFailure = exception;
                MarkPrimaryUnavailable(exception, $"delete '{objectKey}'");
            }
        }

        try
        {
            await _fallback.DeleteAsync(objectKey, cancellationToken);
        }
        catch when (primaryFailure is null)
        {
            throw;
        }
    }

    private CancellationTokenSource CreatePrimaryTimeout(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(_primaryAttemptTimeout);
        return source;
    }

    private void MarkPrimaryUnavailable(Exception exception, string operation)
    {
        _primaryUnavailable = true;
        _logger.LogWarning(
            exception,
            "Amazon S3 {Operation} failed. CivicHero will use local development storage for this request.",
            operation);
    }
}
