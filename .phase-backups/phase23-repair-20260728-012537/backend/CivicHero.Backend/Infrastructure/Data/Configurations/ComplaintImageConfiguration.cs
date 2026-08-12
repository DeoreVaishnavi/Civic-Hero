using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintImageConfiguration : IEntityTypeConfiguration<ComplaintImage>
{
    public void Configure(EntityTypeBuilder<ComplaintImage> builder)
    {
        builder.ToTable("complaint_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.S3Key).HasMaxLength(1024).IsRequired();
        builder.HasIndex(x => x.S3Key).IsUnique();
        builder.Property(x => x.S3Url).HasMaxLength(2048);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        builder.HasOne(x => x.Complaint).WithMany(x => x.Images).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ComplaintId);
    }
}
