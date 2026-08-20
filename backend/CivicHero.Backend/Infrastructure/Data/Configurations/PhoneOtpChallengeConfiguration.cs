using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class PhoneOtpChallengeConfiguration : IEntityTypeConfiguration<PhoneOtpChallenge>
{
    public void Configure(EntityTypeBuilder<PhoneOtpChallenge> builder)
    {
        builder.ToTable("phone_otp_challenges");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Purpose).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RequestedIpHash).HasMaxLength(128);
        builder.Property(entity => entity.ProviderMessageId).HasMaxLength(100);
        builder.HasIndex(entity => new { entity.PhoneNumber, entity.Purpose, entity.CreatedAt });
        builder.HasIndex(entity => entity.ExpiresAt);
        builder.HasOne(entity => entity.User).WithMany(entity => entity.PhoneOtpChallenges)
            .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
