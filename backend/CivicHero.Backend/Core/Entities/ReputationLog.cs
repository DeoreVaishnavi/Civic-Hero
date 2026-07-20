using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a single reputation points transaction.
/// Acts as an immutable ledger entry.
/// </summary>
public sealed class ReputationLog : AuditableEntity
{
    /// <summary>
    /// User associated with this transaction.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Positive or negative point adjustment.
    /// </summary>
    public int Points { get; private set; }

    /// <summary>
    /// Business reason for the transaction.
    /// </summary>
    public string Reason { get; private set; }

    private ReputationLog()
    {
        User = null!;
        Reason = string.Empty;
    }

    /// <summary>
    /// Creates a new reputation transaction.
    /// </summary>
    public ReputationLog(
        Guid userId,
        int points,
        string reason)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (points == 0)
            throw new ArgumentException("Points cannot be zero.", nameof(points));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        UserId = userId;
        Points = points;
        Reason = reason.Trim();
    }
}