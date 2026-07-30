using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            builder.HasKey(cm => cm.Id);

            builder.Property(cm => cm.Id)
                .ValueGeneratedOnAdd();

            builder.Property(cm => cm.Content)
                .IsRequired()
                .HasMaxLength(2000); // Adjust as needed

            builder.Property(cm => cm.CreatedAt)
                .IsRequired();

            builder.Property(cm => cm.Sender)
                .IsRequired()
                .HasConversion<string>() // Store as string in DB
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(cm => cm.User)
                .WithMany() // User does not have a navigation collection for chat messages
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cm => cm.ChatSession)
                .WithMany(cs => cs.Messages)
                .HasForeignKey(cm => cm.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}