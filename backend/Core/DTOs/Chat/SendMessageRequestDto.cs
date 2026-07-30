namespace CivicHero.Backend.Core.DTOs.Chat
{
    public class SendMessageRequestDto
    {
        public string Content { get; set; } = string.Empty;
        public int? ComplaintId { get; set; } // Optional, for context
    }
}