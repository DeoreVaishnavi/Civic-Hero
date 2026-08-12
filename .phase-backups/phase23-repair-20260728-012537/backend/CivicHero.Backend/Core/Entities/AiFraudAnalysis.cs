using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class AiFraudAnalysis : BaseEntity
{
    public long ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public float FraudScore { get; set; }
    public string Verdict { get; set; } = string.Empty;
    public string? Reasoning { get; set; }
    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;
}
