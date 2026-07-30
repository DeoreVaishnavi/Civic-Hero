using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Chat
{
    public class ChatMessageResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public MessageSender Sender { get; set; } = MessageSender.User;
        public string? Username { get; set; } // Optional: for displaying user name
    }
}