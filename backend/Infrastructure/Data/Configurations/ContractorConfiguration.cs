using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("contractors");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CompanyName).HasMaxLength(180).IsRequired();
        builder.Property(entity => entity.ContactPerson).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Phone).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Email).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(entity => new { entity.DepartmentId, entity.Status });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
        builder.HasOne(entity => entity.Department)
            .WithMany(entity => entity.Contractors)
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
