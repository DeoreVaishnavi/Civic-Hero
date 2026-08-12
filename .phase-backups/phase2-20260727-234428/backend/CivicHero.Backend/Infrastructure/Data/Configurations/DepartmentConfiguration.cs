using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(1000);
    }
}
