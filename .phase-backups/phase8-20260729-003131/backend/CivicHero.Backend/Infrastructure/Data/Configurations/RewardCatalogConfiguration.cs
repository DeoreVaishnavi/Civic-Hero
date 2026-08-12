using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class RewardCatalogConfiguration : IEntityTypeConfiguration<RewardCatalog>
{
    public void Configure(EntityTypeBuilder<RewardCatalog> builder)
    {
        builder.ToTable("reward_catalog");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(entity => new { entity.IsActive, entity.PointsCost });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
