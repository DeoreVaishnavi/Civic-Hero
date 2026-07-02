namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Represents the lifecycle of a dispute.
/// </summary>
public enum DisputeStatus
{
    /// <summary>
    /// Dispute has been submitted and is awaiting review.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Dispute is currently being investigated.
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Additional information has been requested.
    /// </summary>
    MoreInformationRequired = 3,

    /// <summary>
    /// Dispute has been approved.
    /// </summary>
    Approved = 4,

    /// <summary>
    /// Dispute has been rejected.
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// Dispute has been closed.
    /// </summary>
    Closed = 6
}