using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintImageConfiguration
    : IEntityTypeConfiguration<ComplaintImage>
{
    public void Configure(EntityTypeBuilder<ComplaintImage> builder)
    {
        builder.ToTable("ComplaintImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.StoredFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.MetadataAnalyzed)
            .IsRequired();

        builder.HasOne(x => x.Complaint)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.MetadataAnalyzed);
    }
}