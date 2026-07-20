using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Notification.
/// </summary>
public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("Notifications");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.IsRead)
            .IsRequired();

        //-----------------------------------------------------
        // Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.Type);

        builder.HasIndex(x => x.IsRead);

        builder.HasIndex(x => x.CreatedOnUtc);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.IsRead
        });

        builder.HasIndex(x => new
        {
            x.UserId,
            x.CreatedOnUtc
        });
    }
}