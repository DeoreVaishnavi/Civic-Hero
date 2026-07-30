namespace CivicHero.Backend.Core.DTOs.Chat
{
    public class ChatSessionResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}