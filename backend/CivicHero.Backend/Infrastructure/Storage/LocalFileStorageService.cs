using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Storage;

public sealed class LocalFileStorageService : IStorageService
{
    private readonly string _rootPath;
    private readonly StringComparison _pathComparison;

    public LocalFileStorageService(
        IHostEnvironment environment,
        IOptions<StorageOptions> options)
    {
        var configuredRoot = string.IsNullOrWhiteSpace(options.Value.LocalRootPath)
            ? "App_Data/uploads"
            : options.Value.LocalRootPath.Trim();

        _rootPath = Path.IsPathRooted(configuredRoot)
            ? Path.GetFullPath(configuredRoot)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));

        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public string Provider => "Local";
    public string BucketName => _rootPath;

    public Task<bool> IsBucketAccessibleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_rootPath);
        return Task.FromResult(true);
    }

    public async Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var path = ResolveObjectPath(objectKey);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Unable to resolve the local upload directory.");

        Directory.CreateDirectory(directory);
        await using var output = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.CopyToAsync(output, cancellationToken);
    }

    public Task<StorageDownload?> DownloadAsync(
        string objectKey,
        string downloadFileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveObjectPath(objectKey);
        if (!File.Exists(path)) return Task.FromResult<StorageDownload?>(null);

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<StorageDownload?>(new StorageDownload(
            stream,
            ResolveContentType(downloadFileName),
            downloadFileName));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveObjectPath(objectKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolveObjectPath(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Storage object key is required.", nameof(objectKey));

        var relative = objectKey
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relative));
        var protectedRoot = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(protectedRoot, _pathComparison))
            throw new InvalidOperationException("The storage object key resolves outside the configured upload directory.");

        return fullPath;
    }

    private static string ResolveContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
}
