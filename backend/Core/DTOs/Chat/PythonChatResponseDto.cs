namespace CivicHero.Backend.Core.DTOs.Chat
{
    public class PythonChatResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public string? SessionId { get; set; } // Echo back the session ID if provided
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}