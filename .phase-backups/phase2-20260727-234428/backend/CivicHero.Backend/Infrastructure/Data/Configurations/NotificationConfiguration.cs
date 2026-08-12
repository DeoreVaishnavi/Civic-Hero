using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasColumnType("text").IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.ReferenceType).HasMaxLength(80);
        builder.HasOne(x => x.User).WithMany(x => x.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
