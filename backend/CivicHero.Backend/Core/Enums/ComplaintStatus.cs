namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Represents the lifecycle status of a complaint.
/// </summary>
public enum ComplaintStatus
{
    Draft = 1,

    Submitted = 2,

    UnderReview = 3,

    Assigned = 4,

    InProgress = 5,

    Resolved = 6,

    Rejected = 7,

    Reopened = 8,

    Closed = 9
}