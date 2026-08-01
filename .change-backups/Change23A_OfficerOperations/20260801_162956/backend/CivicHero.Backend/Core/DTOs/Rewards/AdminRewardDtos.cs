namespace CivicHero.Backend.Core.DTOs.Rewards;

public sealed class SaveRewardCatalogRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsCost { get; set; }
    public string Type { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AdjustRewardStockRequest
{
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class ManualPointsAdjustmentRequest
{
    public long UserId { get; set; }
    public int PointsDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateRedemptionStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminRedemptionQuery
{
    public string? Status { get; set; }
    public int Take { get; set; } = 100;
}

public sealed class BadgeRuleDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "★";
    public string Metric { get; set; } = "Points";
    public int Target { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public sealed class TierRuleDto
{
    public string Name { get; set; } = string.Empty;
    public int MinimumPoints { get; set; }
}

public sealed class SaveBadgeRulesRequest
{
    public IReadOnlyList<BadgeRuleDto> Rules { get; set; } = Array.Empty<BadgeRuleDto>();
}

public sealed class SaveTierRulesRequest
{
    public IReadOnlyList<TierRuleDto> Rules { get; set; } = Array.Empty<TierRuleDto>();
}

public sealed class RewardRulesResponse
{
    public IReadOnlyList<BadgeRuleDto> BadgeRules { get; set; } = Array.Empty<BadgeRuleDto>();
    public IReadOnlyList<TierRuleDto> TierRules { get; set; } = Array.Empty<TierRuleDto>();
}

public sealed class AdminRewardCatalogResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsCost { get; set; }
    public string Type { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AdminRedemptionResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public string CitizenEmail { get; set; } = string.Empty;
    public long RewardCatalogId { get; set; }
    public string RewardName { get; set; } = string.Empty;
    public int PointsSpent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RedemptionCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FulfilledAt { get; set; }
}

public sealed class ManualPointsAdjustmentResponse
{
    public long UserId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public int PointsDelta { get; set; }
    public int NewBalance { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
