using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintEmergencyReviewConfiguration : IEntityTypeConfiguration<ComplaintEmergencyReview>
{
    public void Configure(EntityTypeBuilder<ComplaintEmergencyReview> builder)
    {
        builder.ToTable("complaint_emergency_reviews");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.OriginalPriority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.ConfirmedPriority).HasConversion<string>().HasMaxLength(20);
        builder.Property(entity => entity.ReporterReason).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.DecisionReason).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.Status, entity.CreatedAt });
        builder.HasIndex(entity => entity.ComplaintId);
        builder.HasOne(entity => entity.Complaint).WithMany(entity => entity.EmergencyReviews)
            .HasForeignKey(entity => entity.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.ReviewedByUser).WithMany()
            .HasForeignKey(entity => entity.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
