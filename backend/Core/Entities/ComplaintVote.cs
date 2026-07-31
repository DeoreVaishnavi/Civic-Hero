using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintVote : BaseEntity
{
    public long ComplaintId { get; set; }
    public long UserId { get; set; }
    public DateTimeOffset VotedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
    public User User { get; set; } = null!;
}
