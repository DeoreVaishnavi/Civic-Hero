namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Supported values: S3, Local, S3WithLocalFallback.
    /// </summary>
    public string Provider { get; set; } = "S3";

    /// <summary>
    /// Relative paths are resolved from the backend content root.
    /// </summary>
    public string LocalRootPath { get; set; } = "App_Data/uploads";

    /// <summary>
    /// Maximum time spent trying S3 before the development fallback is used.
    /// </summary>
    public int PrimaryAttemptTimeoutSeconds { get; set; } = 5;
}
