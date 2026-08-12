using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(x => x.Department).WithMany(x => x.Users).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Ward).WithMany(x => x.Users).HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DepartmentId, x.WardId, x.Role });
    }
}
