namespace CivicHero.Backend.Core.DTOs.Chatbot;

public sealed class ChatMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
