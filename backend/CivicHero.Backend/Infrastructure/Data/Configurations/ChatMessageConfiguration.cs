using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Sender).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Content).HasColumnType("text").IsRequired();
        builder.HasIndex(entity => new { entity.ChatSessionId, entity.SentAt });
        builder.HasOne(entity => entity.ChatSession)
            .WithMany(entity => entity.Messages)
            .HasForeignKey(entity => entity.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
