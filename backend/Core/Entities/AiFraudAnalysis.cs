using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class AiFraudAnalysis : BaseEntity
{
    public long ComplaintId { get; set; }
    public float FraudScore { get; set; }
    public string Verdict { get; set; } = string.Empty;
    public string? Reasoning { get; set; }
    public string Provider { get; set; } = "RuleBased";
    public string Model { get; set; } = "civichero-rules-v1";
    public bool RequiresManualReview { get; set; }
    public DateTimeOffset AnalyzedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
}
