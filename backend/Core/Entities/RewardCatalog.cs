using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class RewardCatalog : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsCost { get; set; }
    public RewardType Type { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
}
