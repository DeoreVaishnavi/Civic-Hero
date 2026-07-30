using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintImageConfiguration : IEntityTypeConfiguration<ComplaintImage>
{
    public void Configure(EntityTypeBuilder<ComplaintImage> builder)
    {
        builder.ToTable("complaint_images");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.S3Key).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.S3Url).HasMaxLength(2048);
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.ComplaintId);
        builder.HasIndex(entity => entity.S3Key).IsUnique();
        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.Images)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
