namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Defines all application roles used for
/// authentication and role-based authorization.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular citizen who reports complaints,
    /// verifies resolutions, earns rewards,
    /// and interacts with the chatbot.
    /// </summary>
    Citizen = 1,

    /// <summary>
    /// Government officer responsible for
    /// managing and resolving complaints.
    /// </summary>
    Officer = 2,

    /// <summary>
    /// Department administrator responsible for
    /// supervising officers, contractors,
    /// analytics, and departmental operations.
    /// </summary>
    DepartmentAdmin = 3,

    /// <summary>
    /// Contractor assigned to execute field work
    /// and upload resolution evidence.
    /// </summary>
    Contractor = 4,

    /// <summary>
    /// System administrator with unrestricted
    /// access to every module and configuration.
    /// </summary>
    SystemAdmin = 5
}