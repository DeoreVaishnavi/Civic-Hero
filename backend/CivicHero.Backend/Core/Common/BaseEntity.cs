namespace CivicHero.Backend.Core.Common;

/// <summary>
/// Represents the root entity for the domain model.
/// Every persistent entity in the system inherits from this class.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// A new Guid is generated automatically.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// Two entities are equal when they have the same Id
    /// and belong to the same concrete type.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType()
               && Id == other.Id;
    }

    /// <summary>
    /// Returns the hash code for the entity.
    /// </summary>
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