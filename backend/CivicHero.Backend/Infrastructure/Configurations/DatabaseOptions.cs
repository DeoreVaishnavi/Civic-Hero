namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ServerVersion { get; set; } = "8.0.40";
    public int CommandTimeout { get; set; } = 30;
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 5;
    public int MaxRetryDelaySeconds { get; set; } = 30;
    public bool EnableDetailedErrors { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
    public bool ApplyMigrationsOnStartup { get; set; }
}
