using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Events;

/// <summary>
/// Raised when a new user successfully registers.
/// </summary>
public sealed class UserRegisteredEvent : DomainEvent
{
    /// <summary>
    /// Registered user identifier.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// User email address.
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// User role.
    /// </summary>
    public UserRole Role { get; }

    /// <summary>
    /// Registration timestamp.
    /// </summary>
    public DateTime RegisteredOnUtc { get; }

    public UserRegisteredEvent(
        Guid userId,
        string email,
        UserRole role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        UserId = userId;
        Email = email.Trim();
        Role = role;
        RegisteredOnUtc = DateTime.UtcNow;
    }
}