using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class Complaint : SoftDeleteEntity
{
    public long CitizenId { get; set; }
    public long? AssignedOfficerId { get; set; }
    public long DepartmentId { get; set; }
    public long WardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Created;
    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public bool IsAnonymous { get; set; }
    public bool PossibleEmergency { get; set; }
    public EmergencyReviewStatus EmergencyReviewStatus { get; set; } = EmergencyReviewStatus.NotRequested;
    public DateTimeOffset? EmergencyRequestedAt { get; set; }

    public DateTimeOffset? AiTriagedAt { get; set; }
    public string? AiSuggestedCategory { get; set; }
    public decimal? AiConfidence { get; set; }
    public decimal? AiRiskScore { get; set; }
    public long? DuplicateOfComplaintId { get; set; }

    public User Citizen { get; set; } = null!;
    public User? AssignedOfficer { get; set; }
    public Department Department { get; set; } = null!;
    public Ward Ward { get; set; } = null!;
    public ICollection<ComplaintImage> Images { get; set; } = new List<ComplaintImage>();
    public ICollection<ComplaintTimeline> Timeline { get; set; } = new List<ComplaintTimeline>();
    public ICollection<ComplaintVote> Votes { get; set; } = new List<ComplaintVote>();
    public ICollection<ComplaintAssignment> Assignments { get; set; } = new List<ComplaintAssignment>();
    public ICollection<ComplaintProgressUpdate> ProgressUpdates { get; set; } = new List<ComplaintProgressUpdate>();
    public ICollection<ComplaintVerification> Verifications { get; set; } = new List<ComplaintVerification>();
    public ICollection<AiFraudAnalysis> FraudAnalyses { get; set; } = new List<AiFraudAnalysis>();
    public ICollection<AiTriageAnalysis> TriageAnalyses { get; set; } = new List<AiTriageAnalysis>();
    public ICollection<DisputeAuditLog> Disputes { get; set; } = new List<DisputeAuditLog>();
    public AnonymousComplaintAccess? AnonymousAccess { get; set; }
    public ICollection<ComplaintComment> Comments { get; set; } = new List<ComplaintComment>();
    public ICollection<ComplaintEmergencyReview> EmergencyReviews { get; set; } = new List<ComplaintEmergencyReview>();
    public ICollection<VisualVerificationAnalysis> VisualVerificationAnalyses { get; set; } = new List<VisualVerificationAnalysis>();
}
