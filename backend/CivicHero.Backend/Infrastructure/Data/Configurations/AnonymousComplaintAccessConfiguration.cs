using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class AnonymousComplaintAccessConfiguration : IEntityTypeConfiguration<AnonymousComplaintAccess>
{
    public void Configure(EntityTypeBuilder<AnonymousComplaintAccess> builder)
    {
        builder.ToTable("anonymous_complaint_access");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.TrackingTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.ContactEmailProtected).HasMaxLength(2048);
        builder.Property(entity => entity.ContactPhoneProtected).HasMaxLength(2048);
        builder.Property(entity => entity.ContactHint).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.CaptchaProvider).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.SubmissionIpHash).HasMaxLength(128);
        builder.Property(entity => entity.CaptchaScore).HasPrecision(5, 4);
        builder.HasIndex(entity => entity.ComplaintId).IsUnique();
        builder.HasIndex(entity => entity.TrackingTokenHash).IsUnique();
        builder.HasIndex(entity => entity.TrackingExpiresAt);
        builder.HasIndex(entity => new { entity.SubmissionIpHash, entity.CreatedAt });
        builder.HasOne(entity => entity.Complaint).WithOne(entity => entity.AnonymousAccess)
            .HasForeignKey<AnonymousComplaintAccess>(entity => entity.ComplaintId).OnDelete(DeleteBehavior.Cascade);
    }
}
