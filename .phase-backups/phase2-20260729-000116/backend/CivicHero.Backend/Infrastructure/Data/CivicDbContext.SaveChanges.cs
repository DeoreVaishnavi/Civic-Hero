using CivicHero.Backend.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

public sealed partial class CivicDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyEntityMetadata();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyEntityMetadata();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyEntityMetadata()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = entry.Entity.CreatedAt == default
                    ? now
                    : entry.Entity.CreatedAt;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<SoftDeleteEntity>()
                     .Where(item => item.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.UpdatedAt = now;
        }
    }
}
