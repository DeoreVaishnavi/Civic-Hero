namespace CivicHero.Backend.Core.Common;

/// <summary>
/// Represents the base class for all domain events.
/// A domain event captures something important that has happened
/// within the business domain.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Initializes a new domain event.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }
}