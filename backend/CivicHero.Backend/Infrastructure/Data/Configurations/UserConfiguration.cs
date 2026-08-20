using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Email).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.FullName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Phone).HasMaxLength(20);
        builder.Property(entity => entity.NormalizedPhone).HasMaxLength(20);
        builder.Property(entity => entity.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AuthorizationVersion).HasDefaultValue(1).IsRequired();
        builder.Property(entity => entity.EmailVerificationTokenHash).HasMaxLength(128);
        builder.Property(entity => entity.RefreshTokenHash).HasMaxLength(128);
        builder.Property(entity => entity.TwoFactorSecretProtected).HasMaxLength(2048);
        builder.Property(entity => entity.TwoFactorRecoveryCodesJson).HasColumnType("text");
        builder.HasIndex(entity => entity.Email).IsUnique();
        builder.HasIndex(entity => entity.NormalizedPhone).IsUnique();
        builder.HasIndex(entity => entity.IsSystemAccount);
        builder.HasIndex(entity => entity.RefreshTokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.DepartmentId, entity.WardId, entity.Role });
        builder.HasQueryFilter(entity => !entity.IsDeleted);

        builder.HasOne(entity => entity.Department)
            .WithMany(entity => entity.Users)
            .HasForeignKey(entity => entity.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entity => entity.Ward)
            .WithMany(entity => entity.Users)
            .HasForeignKey(entity => entity.WardId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
