using CivicHero.Backend.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CivicHero.Backend.Infrastructure.Data;

/// <summary>
/// Contains SaveChanges pipeline,
/// audit handling,
/// soft delete support,
/// and domain event collection.
/// </summary>
public sealed partial class CivicDbContext
{
    /// <summary>
    /// Executes all pre-save operations.
    /// </summary>
    private void PrepareEntitiesForSave()
    {
        UpdateAuditInformation();

        ApplySoftDelete();
    }

    /// <summary>
    /// Updates audit information for all tracked entities.
    /// </summary>
    private void UpdateAuditInformation()
    {
        var currentUserId = _currentUserService.UserId;

        foreach (EntityEntry<AuditableEntity> entry in
                 ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:

                    entry.Entity.MarkAsCreated(currentUserId);

                    break;

                case EntityState.Modified:

                    entry.Entity.MarkAsModified(currentUserId);

                    break;
            }
        }
    }

    /// <summary>
    /// Converts delete operations into soft deletes.
    /// </summary>
    private void ApplySoftDelete()
    {
        var currentUserId = _currentUserService.UserId;

        foreach (EntityEntry<SoftDeleteEntity> entry in
                 ChangeTracker.Entries<SoftDeleteEntity>())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            entry.State = EntityState.Modified;

            entry.Entity.MarkAsDeleted(currentUserId);
        }
    }

    /// <summary>
    /// Collects all domain events from tracked entities.
    /// </summary>
    private List<DomainEvent> CollectDomainEvents()
    {
        return ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .SelectMany(x => x.DomainEvents)
            .ToList();
    }

    /// <summary>
    /// Saves all changes.
    /// </summary>
    public override int SaveChanges()
    {
        PrepareEntitiesForSave();

        List<DomainEvent> domainEvents = CollectDomainEvents();

        int result = base.SaveChanges();

        ClearDomainEvents();

        // Domain events will be dispatched
        // in the Application layer.

        return result;
    }

    /// <summary>
    /// Saves all changes asynchronously.
    /// </summary>
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        PrepareEntitiesForSave();

        List<DomainEvent> domainEvents = CollectDomainEvents();

        int result =
            await base.SaveChangesAsync(cancellationToken);

        ClearDomainEvents();

        // Domain events will be dispatched
        // in the Application layer.

        return result;
    }

    /// <summary>
    /// Clears all collected domain events.
    /// </summary>
    private void ClearDomainEvents()
    {
        foreach (BaseEntity entity in ChangeTracker
                     .Entries<BaseEntity>()
                     .Select(x => x.Entity))
        {
            entity.ClearDomainEvents();
        }
    }
}