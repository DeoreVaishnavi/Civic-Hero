using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class AiTriageAnalysis : BaseEntity
{
    public long ComplaintId { get; set; }
    public string Provider { get; set; } = "RuleBased";
    public string Model { get; set; } = "civichero-rules-v1";
    public string PredictedCategory { get; set; } = string.Empty;
    public long? PredictedDepartmentId { get; set; }
    public decimal ClassificationConfidence { get; set; }
    public string DuplicateStatus { get; set; } = "Unique";
    public long? DuplicateComplaintId { get; set; }
    public decimal DuplicateScore { get; set; }
    public string FraudVerdict { get; set; } = "Safe";
    public decimal FraudScore { get; set; }
    public ComplaintPriority PredictedPriority { get; set; } = ComplaintPriority.Medium;
    public decimal PriorityScore { get; set; }
    public bool RequiresManualReview { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string? RawProviderResponse { get; set; }
    public DateTimeOffset AnalyzedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewDecision { get; set; }
    public string? ReviewNotes { get; set; }

    public Complaint Complaint { get; set; } = null!;
}
