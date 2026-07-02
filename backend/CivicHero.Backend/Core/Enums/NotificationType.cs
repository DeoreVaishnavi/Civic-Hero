namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Defines the type of notification sent to a user.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// General system notification.
    /// </summary>
    General = 1,

    /// <summary>
    /// Complaint status has changed.
    /// </summary>
    ComplaintStatusChanged = 2,

    /// <summary>
    /// Complaint has been assigned.
    /// </summary>
    ComplaintAssigned = 3,

    /// <summary>
    /// Complaint has been resolved.
    /// </summary>
    ComplaintResolved = 4,

    /// <summary>
    /// A dispute requires user attention.
    /// </summary>
    DisputeUpdate = 5,

    /// <summary>
    /// Reward points have been earned.
    /// </summary>
    RewardEarned = 6,

    /// <summary>
    /// Reward has been redeemed.
    /// </summary>
    RewardRedeemed = 7,

    /// <summary>
    /// Administrative announcement.
    /// </summary>
    Announcement = 8,

    /// <summary>
    /// Security-related notification.
    /// </summary>
    SecurityAlert = 9
}