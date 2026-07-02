namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Defines the priority level of a complaint.
/// Used for sorting, assignment, SLA tracking,
/// analytics, and reporting.
/// </summary>
public enum ComplaintPriority
{
    Low = 1,

    Medium = 2,

    High = 3,

    Critical = 4
}