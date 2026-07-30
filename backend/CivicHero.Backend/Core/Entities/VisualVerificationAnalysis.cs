using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class VisualVerificationAnalysis : AuditableEntity
{
    public long ComplaintId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public VisualVerificationVerdict Verdict { get; set; } = VisualVerificationVerdict.NeedsHumanReview;
    public decimal CompletionScore { get; set; }
    public decimal ImageQualityScore { get; set; }
    public decimal ManipulationRiskScore { get; set; }
    public decimal OverallConfidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string ObservationsJson { get; set; } = "[]";
    public string BeforeImageIdsJson { get; set; } = "[]";
    public string AfterImageIdsJson { get; set; } = "[]";
    public string EvidenceFingerprint { get; set; } = string.Empty;
    public string? RawResponse { get; set; }
    public bool RequiresHumanReview { get; set; } = true;
    public string? HumanDecision { get; set; }
    public string? HumanNotes { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
