using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class DisputeAuditLogConfiguration : IEntityTypeConfiguration<DisputeAuditLog>
{
    public void Configure(EntityTypeBuilder<DisputeAuditLog> builder)
    {
        builder.ToTable("dispute_audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.CitizenRemarks).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.SupervisorRemarks).HasMaxLength(2000);
        builder.Property(x => x.AdminRemarks).HasMaxLength(2000);
        builder.HasOne(x => x.Complaint).WithMany(x => x.DisputeAuditLogs).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RaisedByUser).WithMany().HasForeignKey(x => x.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
