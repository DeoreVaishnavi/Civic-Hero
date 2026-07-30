using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
    {
        public void Configure(EntityTypeBuilder<ChatSession> builder)
        {
            builder.ToTable("ChatSessions");

            builder.HasKey(cs => cs.Id);

            builder.Property(cs => cs.Id)
                .ValueGeneratedOnAdd();

            builder.Property(cs => cs.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(cs => cs.CreatedAt)
                .IsRequired();

            builder.Property(cs => cs.UpdatedAt)
                .IsRequired(false);

            builder.Property(cs => cs.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Relationships
            builder.HasOne(cs => cs.User)
                .WithMany() // User doesn't have a navigation for chats (optional)
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(cs => cs.Messages)
                .WithOne(m => m.ChatSession)
                .HasForeignKey(m => m.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}