using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents an immutable audit log entry for dispute-related actions.
/// </summary>
public sealed class DisputeAuditLog : AuditableEntity
{
    /// <summary>
    /// Related complaint identifier.
    /// </summary>
    public Guid ComplaintId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Complaint Complaint { get; private set; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Audit action.
    /// Example:
    /// Dispute Created
    /// Evidence Uploaded
    /// Dispute Closed
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Additional audit details.
    /// </summary>
    public string Details { get; private set; }

    /// <summary>
    /// UTC timestamp when the audit entry occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; private set; }

    private DisputeAuditLog()
    {
        Complaint = null!;
        User = null!;
        Action = string.Empty;
        Details = string.Empty;
    }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    public DisputeAuditLog(
        Guid complaintId,
        Guid userId,
        string action,
        string details)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        if (string.IsNullOrWhiteSpace(details))
            throw new ArgumentException("Details are required.", nameof(details));

        ComplaintId = complaintId;
        UserId = userId;
        Action = action.Trim();
        Details = details.Trim();
        OccurredOnUtc = DateTime.UtcNow;
    }
}