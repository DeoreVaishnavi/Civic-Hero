namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public int CommandTimeout { get; set; }

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }
}