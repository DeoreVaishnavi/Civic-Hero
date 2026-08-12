using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class DisputeAuditLog : BaseEntity
{
    public long ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public long RaisedByUserId { get; set; }
    public User RaisedByUser { get; set; } = null!;
    public long? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Raised;
    public string CitizenRemarks { get; set; } = string.Empty;
    public string? SupervisorRemarks { get; set; }
    public string? AdminRemarks { get; set; }
    public DateTimeOffset RaisedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
