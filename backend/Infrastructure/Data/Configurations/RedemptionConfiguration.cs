using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class RedemptionConfiguration : IEntityTypeConfiguration<Redemption>
{
    public void Configure(EntityTypeBuilder<Redemption> builder)
    {
        builder.ToTable("redemptions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.RedemptionCode).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.RedemptionCode).IsUnique();
        builder.HasIndex(entity => new { entity.UserId, entity.CreatedAt });
        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.Redemptions)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.RewardCatalog)
            .WithMany(entity => entity.Redemptions)
            .HasForeignKey(entity => entity.RewardCatalogId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
