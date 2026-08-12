namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string MySqlVersion { get; set; } = "8.0.36";
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool ApplyMigrationsOnStartup { get; set; }
    public bool SeedDataOnStartup { get; set; }
}
