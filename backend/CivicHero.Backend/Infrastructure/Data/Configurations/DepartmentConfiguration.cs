using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Data.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasQueryFilter(entity => !entity.IsDeleted);
        builder.HasData(DepartmentSeed.GetDepartments());
    }
}
