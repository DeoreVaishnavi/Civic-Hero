using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ContractorConfiguration : IEntityTypeConfiguration<Contractor>
{
    public void Configure(EntityTypeBuilder<Contractor> builder)
    {
        builder.ToTable("contractors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(x => x.Department).WithMany(x => x.Contractors).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
