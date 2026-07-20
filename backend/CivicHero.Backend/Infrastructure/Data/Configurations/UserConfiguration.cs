using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for User.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Role)
            .IsRequired();

        builder.Property(x => x.EmailConfirmed)
            .IsRequired();

        builder.Property(x => x.PhoneNumberConfirmed)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Email Value Object
        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                 .HasColumnName("Email")
                 .HasMaxLength(255)
                 .IsRequired();

            email.HasIndex(e => e.Value)
                 .IsUnique();
        });

        builder.HasIndex(x => x.PhoneNumber)
            .IsUnique();

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}