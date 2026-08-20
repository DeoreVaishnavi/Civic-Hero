using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintDraftConfiguration : IEntityTypeConfiguration<ComplaintDraft>
{
    public void Configure(EntityTypeBuilder<ComplaintDraft> builder)
    {
        builder.ToTable("complaint_drafts");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Title).HasMaxLength(200);
        builder.Property(entity => entity.Description).HasColumnType("text");
        builder.Property(entity => entity.Category).HasMaxLength(80);
        builder.Property(entity => entity.CitizenSeverity).HasMaxLength(20).HasDefaultValue("Medium").IsRequired();
        builder.Property(entity => entity.Latitude).HasPrecision(10, 7);
        builder.Property(entity => entity.Longitude).HasPrecision(10, 7);
        builder.Property(entity => entity.Address).HasMaxLength(400);
        builder.Property(entity => entity.Landmark).HasMaxLength(100);
        builder.Property(entity => entity.EmergencyReason).HasMaxLength(1000);
        builder.HasIndex(entity => entity.CitizenId).IsUnique();
        builder.HasIndex(entity => new { entity.DepartmentId, entity.WardId });

        builder.HasOne(entity => entity.Citizen)
            .WithOne()
            .HasForeignKey<ComplaintDraft>(entity => entity.CitizenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Department)
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.Ward)
            .WithMany()
            .HasForeignKey(entity => entity.WardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
