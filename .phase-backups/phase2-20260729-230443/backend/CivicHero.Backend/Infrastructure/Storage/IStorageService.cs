namespace CivicHero.Backend.Infrastructure.Storage;

public interface IStorageService
{
    string Provider { get; }
    string BucketName { get; }
    Task<bool> IsBucketAccessibleAsync(CancellationToken cancellationToken = default);
    Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
    Task<StorageDownload?> DownloadAsync(string objectKey, string downloadFileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}

public sealed record StorageDownload(Stream Content, string ContentType, string FileName);
