using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class RewardCatalogConfiguration : IEntityTypeConfiguration<RewardCatalog>
    {
        public void Configure(EntityTypeBuilder<RewardCatalog> builder)
        {
            builder.ToTable("RewardCatalogs");

            builder.HasKey(rc => rc.Id);

            builder.Property(rc => rc.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(rc => rc.Description)
                .HasMaxLength(1000);

            builder.Property(rc => rc.PointsRequired)
                .IsRequired();

            builder.Property(rc => rc.QuantityAvailable)
                .IsRequired();

            builder.Property(rc => rc.IsActive)
                .IsRequired();

            builder.Property(rc => rc.CreatedAt)
                .IsRequired();

            builder.Property(rc => rc.UpdatedAt)
                .IsRequired();

            builder.HasMany(rc => rc.Redemptions)
                .WithOne(r => r.Reward)
                .HasForeignKey(r => r.RewardId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
