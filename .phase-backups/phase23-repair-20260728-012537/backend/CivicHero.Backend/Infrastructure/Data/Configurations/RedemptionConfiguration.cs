using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class RedemptionConfiguration : IEntityTypeConfiguration<Redemption>
{
    public void Configure(EntityTypeBuilder<Redemption> builder)
    {
        builder.ToTable("redemptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.RedemptionCode).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.RedemptionCode).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.Redemptions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RewardCatalog).WithMany(x => x.Redemptions).HasForeignKey(x => x.RewardCatalogId).OnDelete(DeleteBehavior.Restrict);
    }
}
