namespace CivicHero.Backend.Core.Common;

public abstract class SoftDeleteEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
