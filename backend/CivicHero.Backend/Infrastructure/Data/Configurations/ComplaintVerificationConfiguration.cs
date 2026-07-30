using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintVerificationConfiguration
    : IEntityTypeConfiguration<ComplaintVerification>
{
    public void Configure(EntityTypeBuilder<ComplaintVerification> builder)
    {
        builder.ToTable("complaint_verifications");

        builder.HasKey(entity => entity.Id);

        builder.HasIndex(entity => entity.ComplaintId)
            .IsUnique();

        builder.HasIndex(entity => entity.CitizenId);

        builder.HasIndex(entity => new
        {
            entity.Decision,
            entity.DueAt
        });

        builder.Property(entity => entity.Decision)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(entity => entity.Remarks)
            .HasMaxLength(1500);

        builder.Property(entity => entity.SubmittedLatitude)
            .HasPrecision(10, 7);

        builder.Property(entity => entity.SubmittedLongitude)
            .HasPrecision(10, 7);

        builder.HasOne(entity => entity.Complaint)
            .WithMany(complaint => complaint.Verifications)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Citizen)
            .WithMany(user => user.ComplaintVerifications)
            .HasForeignKey(entity => entity.CitizenId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}