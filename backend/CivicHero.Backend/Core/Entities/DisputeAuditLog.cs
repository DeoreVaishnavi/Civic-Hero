using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;
namespace CivicHero.Backend.Core.Entities;
public sealed class DisputeAuditLog : BaseEntity
{
    public long ComplaintId { get; set; }
    public long RaisedByUserId { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Raised;
    public int CycleNumber { get; set; } = 1;
    public string CitizenRemarks { get; set; } = string.Empty;
    public string? SupervisorDecision { get; set; }
    public string? SupervisorRemarks { get; set; }
    public string? AdminDecision { get; set; }
    public string? AdminRemarks { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? AppealDeadline { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public User RaisedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
