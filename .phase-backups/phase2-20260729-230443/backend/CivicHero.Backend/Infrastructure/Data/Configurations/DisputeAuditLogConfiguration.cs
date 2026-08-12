using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CivicHero.Backend.Infrastructure.Data.Configurations;
public sealed class DisputeAuditLogConfiguration : IEntityTypeConfiguration<DisputeAuditLog>
{
 public void Configure(EntityTypeBuilder<DisputeAuditLog> b){b.ToTable("dispute_audit_logs");b.HasKey(x=>x.Id);b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();b.Property(x=>x.CitizenRemarks).HasMaxLength(1500).IsRequired();b.Property(x=>x.SupervisorDecision).HasMaxLength(40);b.Property(x=>x.SupervisorRemarks).HasMaxLength(1500);b.Property(x=>x.AdminDecision).HasMaxLength(40);b.Property(x=>x.AdminRemarks).HasMaxLength(1500);b.HasIndex(x=>new{x.ComplaintId,x.RaisedAt});b.HasOne(x=>x.Complaint).WithMany(x=>x.Disputes).HasForeignKey(x=>x.ComplaintId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x=>x.RaisedByUser).WithMany().HasForeignKey(x=>x.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);b.HasOne(x=>x.ReviewedByUser).WithMany().HasForeignKey(x=>x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);}
}
