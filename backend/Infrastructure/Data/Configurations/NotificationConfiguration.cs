using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Message).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.ReferenceType).HasMaxLength(80);
        builder.Property(entity => entity.ActionUrl).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.UserId, entity.IsRead, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.UserId, entity.IsArchived, entity.CreatedAt });
        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.Notifications)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
