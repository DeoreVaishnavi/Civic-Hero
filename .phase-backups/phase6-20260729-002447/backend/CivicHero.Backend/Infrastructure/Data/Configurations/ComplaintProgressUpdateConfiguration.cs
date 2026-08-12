using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintProgressUpdateConfiguration : IEntityTypeConfiguration<ComplaintProgressUpdate>
{
    public void Configure(EntityTypeBuilder<ComplaintProgressUpdate> builder)
    {
        builder.ToTable("complaint_progress_updates");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Message).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Latitude).HasPrecision(10, 7);
        builder.Property(entity => entity.Longitude).HasPrecision(10, 7);
        builder.HasIndex(entity => new { entity.ComplaintId, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.OfficerId, entity.CreatedAt });

        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.ProgressUpdates)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Officer)
            .WithMany(entity => entity.ProgressUpdates)
            .HasForeignKey(entity => entity.OfficerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
