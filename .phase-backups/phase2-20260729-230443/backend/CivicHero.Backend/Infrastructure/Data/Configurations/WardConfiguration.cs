using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Data.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.BoundaryNorth).HasPrecision(10, 7);
        builder.Property(entity => entity.BoundarySouth).HasPrecision(10, 7);
        builder.Property(entity => entity.BoundaryEast).HasPrecision(10, 7);
        builder.Property(entity => entity.BoundaryWest).HasPrecision(10, 7);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.DepartmentId);
        builder.HasQueryFilter(entity => !entity.IsDeleted);

        builder.HasOne(entity => entity.Department)
            .WithMany(entity => entity.Wards)
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(WardSeed.GetWards());
    }
}
