using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class Redemption : AuditableEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long RewardCatalogId { get; set; }
    public RewardCatalog RewardCatalog { get; set; } = null!;
    public int PointsSpent { get; set; }
    public RedemptionStatus Status { get; set; } = RedemptionStatus.Pending;
    public string RedemptionCode { get; set; } = string.Empty;
    public DateTimeOffset? FulfilledAt { get; set; }
}
