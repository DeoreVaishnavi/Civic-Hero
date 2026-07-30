using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintEmergencyReview : AuditableEntity
{
    public long ComplaintId { get; set; }
    public EmergencyReviewStatus Status { get; set; } = EmergencyReviewStatus.Pending;
    public string ReporterReason { get; set; } = string.Empty;
    public ComplaintPriority OriginalPriority { get; set; } = ComplaintPriority.Medium;
    public ComplaintPriority? ConfirmedPriority { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? DecisionReason { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
