using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.Category).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.EmergencyReviewStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Latitude).HasPrecision(10, 7);
        builder.Property(entity => entity.Longitude).HasPrecision(10, 7);
        builder.Property(entity => entity.Address).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.AiSuggestedCategory).HasMaxLength(80);
        builder.Property(entity => entity.AiConfidence).HasPrecision(5, 4);
        builder.Property(entity => entity.AiRiskScore).HasPrecision(5, 4);
        builder.HasIndex(entity => entity.CitizenId);
        builder.HasIndex(entity => new { entity.DepartmentId, entity.WardId, entity.Status });
        builder.HasIndex(entity => new { entity.Category, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.Latitude, entity.Longitude });
        builder.HasIndex(entity => entity.AiTriagedAt);
        builder.HasIndex(entity => entity.DuplicateOfComplaintId);
        builder.HasIndex(entity => new { entity.IsAnonymous, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.EmergencyReviewStatus, entity.Priority });
        builder.HasQueryFilter(entity => !entity.IsDeleted);

        builder.HasOne(entity => entity.Citizen)
            .WithMany(entity => entity.CreatedComplaints)
            .HasForeignKey(entity => entity.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.AssignedOfficer)
            .WithMany(entity => entity.AssignedComplaints)
            .HasForeignKey(entity => entity.AssignedOfficerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.Department)
            .WithMany(entity => entity.Complaints)
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Ward)
            .WithMany(entity => entity.Complaints)
            .HasForeignKey(entity => entity.WardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
