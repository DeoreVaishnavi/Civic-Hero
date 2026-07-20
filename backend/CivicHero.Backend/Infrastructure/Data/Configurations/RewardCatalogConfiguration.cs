using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for RewardCatalog.
/// </summary>
public sealed class RewardCatalogConfiguration
    : IEntityTypeConfiguration<RewardCatalog>
{
    public void Configure(EntityTypeBuilder<RewardCatalog> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("RewardCatalogs");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.RequiredPoints)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        //-----------------------------------------------------
        // Check Constraint
        //-----------------------------------------------------

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_RewardCatalog_RequiredPoints",
                "`RequiredPoints` > 0");
        });

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasIndex(x => x.Type);

        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => x.RequiredPoints);

        builder.HasIndex(x => new
        {
            x.Type,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.IsActive,
            x.RequiredPoints
        });
    }
}