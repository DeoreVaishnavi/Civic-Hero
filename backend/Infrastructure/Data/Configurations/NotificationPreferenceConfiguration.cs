using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => entity.UserId).IsUnique();
        builder.HasOne(entity => entity.User)
            .WithOne(entity => entity.NotificationPreference)
            .HasForeignKey<NotificationPreference>(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
