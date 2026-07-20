using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a notification delivered to a user.
/// </summary>
public sealed class Notification : AuditableEntity
{
    /// <summary>
    /// Recipient user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public User? User { get; private set; } =null!;

    /// <summary>
    /// Notification title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Notification message.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Notification category.
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Indicates whether the notification has been read.
    /// </summary>
    public bool IsRead { get; private set; }

 private Notification()
{
    User = null!;

    Title = string.Empty;
    Message = string.Empty;
}

    public Notification(
        Guid userId,
        string title,
        string message,
        NotificationType type)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        Type = type;
        IsRead = false;
    }

    /// <summary>
    /// Marks the notification as read.
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }
}