namespace CivicHero.Backend.Core.DTOs.Rewards;

public sealed class PointsBalanceResponse
{
    public int Balance { get; set; }
    public int LifetimeEarned { get; set; }
    public int LifetimeSpent { get; set; }
    public int Rank { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int NextTierAt { get; set; }
}

public sealed class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public long UserId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int ClosedComplaints { get; set; }
    public string Tier { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}

public sealed class LeaderboardCitizenProfileResponse
{
    public long UserId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Points { get; set; }
    public int SubmittedComplaints { get; set; }
    public int ClosedComplaints { get; set; }
    public int HelpfulVerifications { get; set; }
    public int SupportedIssues { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsCurrentUser { get; set; }
    public DateTimeOffset MemberSince { get; set; }
    public IReadOnlyList<string> Badges { get; set; } = Array.Empty<string>();
}

public sealed class BadgeResponse
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
}

public sealed class RewardCatalogResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsCost { get; set; }
    public string Type { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool CanRedeem { get; set; }
}

public sealed class RedeemRewardRequest
{
    public long RewardCatalogId { get; set; }
}

public sealed class RedemptionResponse
{
    public long Id { get; set; }
    public string RewardName { get; set; } = string.Empty;
    public int PointsSpent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RedemptionCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FulfilledAt { get; set; }
}

public sealed class PointsHistoryResponse
{
    public long Id { get; set; }
    public int PointsDelta { get; set; }
    public int RunningBalance { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
