using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintTimeline : BaseEntity
{
    public long ComplaintId { get; set; }
    public long? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    public Complaint Complaint { get; set; } = null!;
    public User? User { get; set; }
}
