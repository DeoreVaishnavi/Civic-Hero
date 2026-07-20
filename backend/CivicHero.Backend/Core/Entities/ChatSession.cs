using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a conversation between a user and the system.
/// </summary>
public sealed class ChatSession : AuditableEntity
{
    /// <summary>
    /// User who owns this chat session.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public User User { get; private set; }

    /// <summary>
    /// Optional complaint associated with this chat.
    /// </summary>
    public Guid? ComplaintId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Complaint? Complaint { get; private set; }

    /// <summary>
    /// Indicates whether the chat session is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// UTC timestamp when the session ended.
    /// </summary>
    public DateTime? ClosedOnUtc { get; private set; }

private ChatSession()
{
    User = null!;
}
    public ChatSession(Guid userId, Guid? complaintId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        UserId = userId;
        ComplaintId = complaintId;
        IsActive = true;
    }

    /// <summary>
    /// Closes the chat session.
    /// </summary>
    public void Close()
    {
        if (!IsActive)
            return;

        IsActive = false;
        ClosedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Reopens the chat session.
    /// </summary>
    public void Reopen()
    {
        IsActive = true;
        ClosedOnUtc = null;
    }
}