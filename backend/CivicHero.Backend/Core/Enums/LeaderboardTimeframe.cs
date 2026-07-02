namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Defines the time period used when calculating
/// leaderboard rankings.
/// </summary>
public enum LeaderboardTimeframe
{
    /// <summary>
    /// Rankings for the current day.
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Rankings for the current week.
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Rankings for the current month.
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Rankings for the current year.
    /// </summary>
    Yearly = 4,

    /// <summary>
    /// Rankings across all available data.
    /// </summary>
    AllTime = 5
}