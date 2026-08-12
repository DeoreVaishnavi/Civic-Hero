using CivicHero.Backend.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

public sealed partial class CivicDbContext
{
    public override int SaveChanges()
    {
        ApplyAuditValues();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditValues()
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
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<SoftDeleteEntity>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.UpdatedAt = now;
        }
    }
}
