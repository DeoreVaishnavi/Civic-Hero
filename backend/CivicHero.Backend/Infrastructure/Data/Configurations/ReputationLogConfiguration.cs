using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ReputationLog.
/// </summary>
public sealed class ReputationLogConfiguration
    : IEntityTypeConfiguration<ReputationLog>
{
    public void Configure(EntityTypeBuilder<ReputationLog> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("ReputationLogs");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.Points)
            .IsRequired();

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

        //-----------------------------------------------------
        // Check Constraint
        //-----------------------------------------------------

    builder.ToTable("ReputationLogs", table =>
{
    table.HasCheckConstraint(
        "CK_ReputationLog_Points",
        "Points <> 0");
});
        //-----------------------------------------------------
        // Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.CreatedOnUtc);

        builder.HasIndex(x => x.Points);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.CreatedOnUtc
        });
    }
}