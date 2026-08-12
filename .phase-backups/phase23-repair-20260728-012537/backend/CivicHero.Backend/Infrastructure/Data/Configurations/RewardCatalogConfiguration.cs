using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class RewardCatalogConfiguration : IEntityTypeConfiguration<RewardCatalog>
{
    public void Configure(EntityTypeBuilder<RewardCatalog> builder)
    {
        builder.ToTable("reward_catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
    }
}
