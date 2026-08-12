namespace CivicHero.Backend.Infrastructure.Storage;

public interface IStorageService
{
    string Provider { get; }
    string BucketName { get; }
    Task<bool> IsBucketAccessibleAsync(CancellationToken cancellationToken = default);
}
