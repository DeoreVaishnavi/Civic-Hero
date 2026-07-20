namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Represents the lifecycle state of a reward redemption.
/// </summary>
public enum RedemptionStatus
{
    /// <summary>
    /// Redemption request has been created and is waiting for processing.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Reward has been successfully redeemed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Redemption request has been cancelled.
    /// </summary>
    Cancelled = 3
}