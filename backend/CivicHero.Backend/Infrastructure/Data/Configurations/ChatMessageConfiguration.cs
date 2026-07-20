using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ChatMessage.
/// </summary>
public sealed class ChatMessageConfiguration
    : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("ChatMessages");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Sender)
            .IsRequired();

        builder.Property(x => x.SentOnUtc)
            .IsRequired();

        //-----------------------------------------------------
        // Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.ChatSession)
            .WithMany()
            .HasForeignKey(x => x.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.ChatSessionId);

        builder.HasIndex(x => x.SentOnUtc);

        builder.HasIndex(x => new
        {
            x.ChatSessionId,
            x.SentOnUtc
        });

        builder.HasIndex(x => x.Sender);
    }
}