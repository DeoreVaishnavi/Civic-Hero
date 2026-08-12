namespace CivicHero.Backend.Infrastructure.Storage;

public interface IStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<string> GetReadUrlAsync(string objectKey, CancellationToken cancellationToken = default);
}
