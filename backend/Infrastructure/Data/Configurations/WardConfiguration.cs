using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("Wards");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(w => w.Code)
            .IsUnique();

        builder.Property(w => w.BoundaryGeoJson)
            .IsRequired(false);

        builder.Property(w => w.CreatedAt)
            .IsRequired();
    }
}