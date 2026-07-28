using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public class ComplaintConfiguration :
    IEntityTypeConfiguration<Complaint>
{
    public void Configure(
        EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(3000);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(3000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.WardId);
        builder.HasIndex(x => x.DepartmentId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.Timeline)
            .WithOne(x => x.Complaint)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}