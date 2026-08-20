using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.SessionId).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.RefreshTokenHash).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.DeviceLabel).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.UserAgent).HasMaxLength(500);
        builder.Property(entity => entity.RevokedReason).HasMaxLength(500);

        builder.HasIndex(entity => entity.SessionId).IsUnique();
        builder.HasIndex(entity => entity.RefreshTokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.UserId, entity.RevokedAt, entity.ExpiresAt });
        builder.HasIndex(entity => new { entity.UserId, entity.AuthorizationVersion });

        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.UserSessions)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
