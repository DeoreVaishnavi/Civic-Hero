using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Events;

/// <summary>
/// Raised when a reward redemption has been completed.
/// </summary>
public sealed class RewardRedeemedEvent : DomainEvent
{
    /// <summary>
    /// Redemption identifier.
    /// </summary>
    public Guid RedemptionId { get; }

    /// <summary>
    /// User who redeemed the reward.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Reward identifier.
    /// </summary>
    public Guid RewardCatalogId { get; }

    /// <summary>
    /// Points spent.
    /// </summary>
    public int PointsSpent { get; }

    /// <summary>
    /// UTC timestamp when the reward was redeemed.
    /// </summary>
    public DateTime RedeemedOnUtc { get; }

    /// <summary>
    /// Creates a new reward redeemed event.
    /// </summary>
    public RewardRedeemedEvent(
        Guid redemptionId,
        Guid userId,
        Guid rewardCatalogId,
        int pointsSpent)
    {
        if (redemptionId == Guid.Empty)
            throw new ArgumentException("Redemption ID is required.", nameof(redemptionId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (rewardCatalogId == Guid.Empty)
            throw new ArgumentException("Reward ID is required.", nameof(rewardCatalogId));

        if (pointsSpent <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(pointsSpent),
                "Points spent must be greater than zero.");

        RedemptionId = redemptionId;
        UserId = userId;
        RewardCatalogId = rewardCatalogId;
        PointsSpent = pointsSpent;
        RedeemedOnUtc = DateTime.UtcNow;
    }
}