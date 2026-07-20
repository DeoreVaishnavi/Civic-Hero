using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ChatSession.
/// </summary>
public sealed class ChatSessionConfiguration
    : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("ChatSessions");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ClosedOnUtc);

        //-----------------------------------------------------
        // User Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //-----------------------------------------------------
        // Complaint Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.Complaint)
            .WithMany()
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.SetNull);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.IsActive);

        builder.HasIndex(x => x.CreatedOnUtc);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.ComplaintId,
            x.IsActive
        });
    }
}