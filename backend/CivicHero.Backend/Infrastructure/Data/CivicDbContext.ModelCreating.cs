using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

/// <summary>
/// Contains all EF Core model configuration.
/// </summary>
public sealed partial class CivicDbContext
{
    /// <summary>
    /// Configures the EF Core model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        //-------------------------------------------------------
        // Ignore domain events (not persisted)
        //-------------------------------------------------------

        IgnoreDomainEvents(modelBuilder);

        //-------------------------------------------------------
        // Apply Fluent API Configurations
        //-------------------------------------------------------

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CivicDbContext).Assembly);

        //-------------------------------------------------------
        // Configure Value Objects
        //-------------------------------------------------------

        ConfigureValueObjects(modelBuilder);

        //-------------------------------------------------------
        // Configure Global Query Filters
        //-------------------------------------------------------

        ConfigureSoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Prevents EF Core from mapping domain events.
    /// </summary>
    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<Contractor>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<Department>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<Ward>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<Complaint>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<ComplaintImage>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<ComplaintTimeline>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<ComplaintVote>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<AiFraudAnalysis>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<DisputeAuditLog>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<Notification>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<ChatSession>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<ChatMessage>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<RewardCatalog>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<ReputationLog>()
            .Ignore(x => x.DomainEvents);

        modelBuilder.Entity<Redemption>()
            .Ignore(x => x.DomainEvents);
    }

    /// <summary>
    /// Configures all owned value objects.
    /// </summary>
    private static void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Complaint>()
            .OwnsOne(x => x.Location);
    }

    /// <summary>
    /// Applies global soft delete filters.
    /// </summary>
    private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Contractor>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<RewardCatalog>()
            .HasQueryFilter(x => !x.IsDeleted);
    }
}