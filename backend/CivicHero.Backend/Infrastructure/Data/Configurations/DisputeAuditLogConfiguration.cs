using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DisputeAuditLog.
/// </summary>
public sealed class DisputeAuditLogConfiguration
    : IEntityTypeConfiguration<DisputeAuditLog>
{
    public void Configure(EntityTypeBuilder<DisputeAuditLog> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("DisputeAuditLogs");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Details)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.OccurredOnUtc)
            .IsRequired();

        //-----------------------------------------------------
        // Relationships
        //-----------------------------------------------------

        builder.HasOne(x => x.Complaint)
            .WithMany()
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.Action);

        builder.HasIndex(x => x.OccurredOnUtc);

        builder.HasIndex(x => new
        {
            x.ComplaintId,
            x.OccurredOnUtc
        });

        builder.HasIndex(x => new
        {
            x.UserId,
            x.OccurredOnUtc
        });
    }
}