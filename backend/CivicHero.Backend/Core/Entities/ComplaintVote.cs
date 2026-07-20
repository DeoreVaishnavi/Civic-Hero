using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a citizen's vote for a complaint.
/// A user may vote only once per complaint.
/// </summary>
public sealed class ComplaintVote : AuditableEntity
{
    /// <summary>
    /// Related complaint identifier.
    /// </summary>
    public Guid ComplaintId { get; private set; }

    /// <summary>
    /// Navigation property to the complaint.
    /// </summary>
    public Complaint Complaint { get; private set; }

    /// <summary>
    /// User who cast the vote.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User? User { get; private set; }

 private ComplaintVote()
{
    Complaint = null!;
    User = null!;
}

    /// <summary>
    /// Creates a new complaint vote.
    /// </summary>
    public ComplaintVote(Guid complaintId, Guid userId)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        ComplaintId = complaintId;
        UserId = userId;
    }
}