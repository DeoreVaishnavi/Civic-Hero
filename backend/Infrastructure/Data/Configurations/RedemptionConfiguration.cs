using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class RedemptionConfiguration : IEntityTypeConfiguration<Redemption>
    {
        public void Configure(EntityTypeBuilder<Redemption> builder)
        {
            builder.ToTable("Redemptions");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.PointsSpent)
                .IsRequired();

            builder.Property(r => r.RedeemedAt)
                .IsRequired();

            builder.Property(r => r.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(r => r.Notes)
                .HasMaxLength(500);

            builder.HasOne(r => r.User)
                .WithMany(u => u.Redemptions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Reward)
                .WithMany(rc => rc.Redemptions)
                .HasForeignKey(r => r.RewardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
