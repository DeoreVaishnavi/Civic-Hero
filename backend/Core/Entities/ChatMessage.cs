using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ChatSessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public MessageSender Sender { get; set; } = MessageSender.User;

        // Navigation properties
        public User User { get; set; } = null!;
        public ChatSession ChatSession { get; set; } = null!;
    }
}