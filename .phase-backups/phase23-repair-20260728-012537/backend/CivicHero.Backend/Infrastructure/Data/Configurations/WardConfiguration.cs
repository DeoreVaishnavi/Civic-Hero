using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.BoundaryNorth).HasPrecision(10, 7);
        builder.Property(x => x.BoundarySouth).HasPrecision(10, 7);
        builder.Property(x => x.BoundaryEast).HasPrecision(10, 7);
        builder.Property(x => x.BoundaryWest).HasPrecision(10, 7);
        builder.HasOne(x => x.Department).WithMany(x => x.Wards).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
