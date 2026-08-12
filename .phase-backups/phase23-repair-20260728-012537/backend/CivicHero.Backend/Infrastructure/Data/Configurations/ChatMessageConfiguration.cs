using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sender).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.HasOne(x => x.ChatSession).WithMany(x => x.Messages).HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ChatSessionId, x.SentAt });
    }
}
