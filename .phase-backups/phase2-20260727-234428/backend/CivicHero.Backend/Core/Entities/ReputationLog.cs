using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ReputationLog : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public int PointsDelta { get; set; }
    public int RunningBalance { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
