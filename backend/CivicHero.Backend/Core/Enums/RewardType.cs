namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Defines the category of rewards available
/// in the CivicHero platform.
/// </summary>
public enum RewardType
{
    /// <summary>
    /// Digital certificate issued to the user.
    /// </summary>
    Certificate = 1,

    /// <summary>
    /// Physical gift item.
    /// </summary>
    Merchandise = 2,

    /// <summary>
    /// Shopping or service voucher.
    /// </summary>
    Voucher = 3,

    /// <summary>
    /// Donation made on behalf of the user.
    /// </summary>
    Donation = 4,

    /// <summary>
    /// Recognition badge displayed in the user's profile.
    /// </summary>
    Badge = 5,

    /// <summary>
    /// Custom reward configured by the administrator.
    /// </summary>
    Custom = 6
}