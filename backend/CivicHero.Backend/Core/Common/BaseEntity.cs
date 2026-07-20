using System.ComponentModel.DataAnnotations.Schema;

namespace CivicHero.Backend.Core.Common;

/// <summary>
/// Represents the root entity for the domain model.
/// Every persistent entity in the system inherits from this class.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Stores all domain events raised by the entity.
    /// This collection is not persisted to the database.
    /// </summary>
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets all domain events raised by this entity.
    /// EF Core must ignore this property because
    /// domain events are part of domain logic, not persistence.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<DomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the entity.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Adds a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    protected void RemoveDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType()
               && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !Equals(left, right);
    }
}