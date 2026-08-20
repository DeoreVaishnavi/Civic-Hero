using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Key).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Value).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.ValueType).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.Group).HasMaxLength(80).IsRequired();
        builder.HasIndex(entity => entity.Key).IsUnique();
        builder.HasIndex(entity => entity.Group);
    }
}
