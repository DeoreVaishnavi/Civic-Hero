using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class DisputeAuditLogConfiguration : IEntityTypeConfiguration<DisputeAuditLog>
    {
        public void Configure(EntityTypeBuilder<DisputeAuditLog> builder)
        {
            builder.ToTable("DisputeAuditLogs");

            builder.HasKey(dal => dal.Id);

            builder.Property(dal => dal.ComplaintId)
                .IsRequired();

            builder.Property(dal => dal.InitiatedByUserId)
                .IsRequired();

            builder.Property(dal => dal.Action)
                .IsRequired()
                .HasMaxLength(200); // e.g., "Dispute initiated", "Evidence submitted", "Verdict submitted"

            builder.Property(dal => dal.Details)
                .HasMaxLength(2000); // JSON or text details

            builder.Property(dal => dal.Timestamp)
                .IsRequired();

            // Navigation properties
            builder.HasOne(dal => dal.User)
                .WithMany() // Assuming User has a collection of DisputeAuditLogs, but we don't see it in User entity
                .HasForeignKey(dal => dal.InitiatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}