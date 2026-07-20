using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Events;
using CivicHero.Backend.Core.Exceptions;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a reward redemption performed by a user.
/// </summary>
public sealed class Redemption : AuditableEntity
{
    /// <summary>
    /// User who redeemed the reward.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Reward being redeemed.
    /// </summary>
    public Guid RewardCatalogId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public RewardCatalog RewardCatalog { get; private set; }

    /// <summary>
    /// Points spent for this redemption.
    /// </summary>
    public int PointsSpent { get; private set; }

    /// <summary>
    /// Current redemption status.
    /// </summary>
    public RedemptionStatus Status { get; private set; }

    /// <summary>
    /// UTC date and time when the reward was redeemed.
    /// </summary>
    public DateTime RedeemedOnUtc { get; private set; }

    private Redemption()
    {
        User = null!;
        RewardCatalog = null!;
    }

    /// <summary>
    /// Creates a new reward redemption.
    /// </summary>
    public Redemption(
        Guid userId,
        Guid rewardCatalogId,
        int pointsSpent)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (rewardCatalogId == Guid.Empty)
            throw new ArgumentException("Reward ID is required.", nameof(rewardCatalogId));

        if (pointsSpent <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(pointsSpent),
                "Points spent must be greater than zero.");

        UserId = userId;
        RewardCatalogId = rewardCatalogId;
        PointsSpent = pointsSpent;

        RedeemedOnUtc = DateTime.UtcNow;
        Status = RedemptionStatus.Pending;
    }

    /// <summary>
    /// Marks the redemption as completed.
    /// Raises RewardRedeemedEvent.
    /// </summary>
    public void Complete()
    {
        if (Status == RedemptionStatus.Completed)
            throw new DomainException("This reward has already been redeemed.");

        if (Status == RedemptionStatus.Cancelled)
            throw new DomainException("A cancelled redemption cannot be completed.");

        Status = RedemptionStatus.Completed;

        AddDomainEvent(
            new RewardRedeemedEvent(
                Id,
                UserId,
                RewardCatalogId,
                PointsSpent));
    }

    /// <summary>
    /// Cancels the redemption.
    /// </summary>
    public void Cancel()
    {
        if (Status == RedemptionStatus.Completed)
            throw new DomainException("A completed redemption cannot be cancelled.");

        if (Status == RedemptionStatus.Cancelled)
            return;

        Status = RedemptionStatus.Cancelled;
    }

    /// <summary>
    /// Determines whether the redemption is pending.
    /// </summary>
    public bool IsPending()
    {
        return Status == RedemptionStatus.Pending;
    }

    /// <summary>
    /// Determines whether the redemption is completed.
    /// </summary>
    public bool IsCompleted()
    {
        return Status == RedemptionStatus.Completed;
    }

    /// <summary>
    /// Determines whether the redemption is cancelled.
    /// </summary>
    public bool IsCancelled()
    {
        return Status == RedemptionStatus.Cancelled;
    }
}