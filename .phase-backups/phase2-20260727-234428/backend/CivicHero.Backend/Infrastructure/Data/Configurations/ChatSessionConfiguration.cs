using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.SessionId).IsUnique();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.HasOne(x => x.User).WithMany(x => x.ChatSessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
