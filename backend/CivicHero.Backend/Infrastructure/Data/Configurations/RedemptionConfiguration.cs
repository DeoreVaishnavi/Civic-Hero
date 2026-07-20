using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Redemption.
/// </summary>
public sealed class RedemptionConfiguration
    : IEntityTypeConfiguration<Redemption>
{
    public void Configure(EntityTypeBuilder<Redemption> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

 builder.ToTable("Redemptions", table =>
{
    table.HasCheckConstraint(
        "CK_Redemption_PointsSpent",
        "`PointsSpent` > 0");
});

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.PointsSpent)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.RedeemedOnUtc)
            .IsRequired();

        //-----------------------------------------------------
        // Relationships
        //-----------------------------------------------------

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RewardCatalog)
            .WithMany()
            .HasForeignKey(x => x.RewardCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.RewardCatalogId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.RedeemedOnUtc);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.Status
        });

        builder.HasIndex(x => new
        {
            x.UserId,
            x.RedeemedOnUtc
        });

        builder.HasIndex(x => new
        {
            x.RewardCatalogId,
            x.Status
        });
    }
}