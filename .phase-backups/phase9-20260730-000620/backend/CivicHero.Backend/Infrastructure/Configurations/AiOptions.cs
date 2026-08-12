namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string Provider { get; set; } = "RuleBased";
    public string Model { get; set; } = "gemini-3.6-flash";
    public string? ApiKey { get; set; }
    public bool EnableBackgroundTriage { get; set; } = true;
    public int WorkerIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 20;
    public int DuplicateRadiusMeters { get; set; } = 500;
    public decimal PossibleDuplicateThreshold { get; set; } = 0.72m;
    public decimal ConfirmedDuplicateThreshold { get; set; } = 0.90m;
    public decimal FraudReviewThreshold { get; set; } = 0.65m;
    public int RecentSubmissionWindowHours { get; set; } = 24;
    public int ExcessiveSubmissionCount { get; set; } = 8;

    public bool UseGemini => string.Equals(Provider, "Gemini", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}
