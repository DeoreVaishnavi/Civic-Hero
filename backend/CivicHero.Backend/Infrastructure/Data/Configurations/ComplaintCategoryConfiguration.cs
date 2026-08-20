using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintCategoryConfiguration : IEntityTypeConfiguration<ComplaintCategory>
{
    public void Configure(EntityTypeBuilder<ComplaintCategory> builder)
    {
        builder.ToTable("complaint_categories");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.DefaultPriority).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Icon).HasMaxLength(80);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => new { entity.IsActive, entity.SortOrder });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
        builder.HasOne(entity => entity.Department)
            .WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
