using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintAssignment : BaseEntity
{
    public long ComplaintId { get; set; }
    public long OfficerId { get; set; }
    public long AssignedById { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
    public string? Reason { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public DateTimeOffset? ReassignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset AssignmentDueAt { get; set; }
    public DateTimeOffset ResolutionDueAt { get; set; }
    public bool IsCurrent { get; set; } = true;

    public Complaint Complaint { get; set; } = null!;
    public User Officer { get; set; } = null!;
    public User AssignedBy { get; set; } = null!;
}
