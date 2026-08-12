using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.SessionId).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(30).IsRequired();
        builder.HasIndex(entity => entity.SessionId).IsUnique();
        builder.HasIndex(entity => new { entity.UserId, entity.StartedAt });
        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.ChatSessions)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
