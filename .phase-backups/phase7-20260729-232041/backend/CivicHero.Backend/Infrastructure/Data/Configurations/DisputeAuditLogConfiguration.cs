using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class DisputeAuditLogConfiguration : IEntityTypeConfiguration<DisputeAuditLog>
{
    public void Configure(EntityTypeBuilder<DisputeAuditLog> builder)
    {
        builder.ToTable("dispute_audit_logs");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.CitizenRemarks).HasMaxLength(1500).IsRequired();
        builder.Property(entity => entity.SupervisorRemarks).HasMaxLength(1500);
        builder.Property(entity => entity.AdminRemarks).HasMaxLength(1500);
        builder.HasIndex(entity => new { entity.ComplaintId, entity.RaisedAt });
        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.Disputes)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.RaisedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.RaisedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ReviewedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
