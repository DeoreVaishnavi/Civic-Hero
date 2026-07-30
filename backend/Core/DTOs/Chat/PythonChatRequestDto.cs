namespace CivicHero.Backend.Core.DTOs.Chat
{
    public class PythonChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int? ComplaintId { get; set; } // Optional, for context
        public string? SessionId { get; set; } // Optional, to maintain chat session
    }
}