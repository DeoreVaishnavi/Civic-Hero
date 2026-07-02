namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Represents the operational status of a contractor.
/// Determines whether a contractor can receive complaint assignments.
/// </summary>
public enum ContractorStatus
{
    /// <summary>
    /// Contractor is available to receive new assignments.
    /// </summary>
    Available = 1,

    /// <summary>
    /// Contractor is currently working on assigned complaints.
    /// </summary>
    Busy = 2,

    /// <summary>
    /// Contractor is temporarily unavailable (leave, maintenance, etc.).
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// Contractor has been suspended by the administration.
    /// </summary>
    Suspended = 4,

    /// <summary>
    /// Contractor is no longer active in the system.
    /// </summary>
    Inactive = 5
}