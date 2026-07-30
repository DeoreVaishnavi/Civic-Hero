namespace CivicHero.Backend.Core.DTOs.Chatbot;

public sealed class ChatSessionResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public IReadOnlyList<ChatMessageResponse> Messages { get; init; } = Array.Empty<ChatMessageResponse>();
}

public sealed class ChatMessageResponse
{
    public long Id { get; init; }
    public string Sender { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset SentAt { get; init; }
    public string? ActionLabel { get; init; }
    public string? ActionUrl { get; init; }
}

public sealed class ChatReplyResponse
{
    public string SessionId { get; init; } = string.Empty;
    public ChatMessageResponse UserMessage { get; init; } = new();
    public ChatMessageResponse AssistantMessage { get; init; } = new();
    public string Intent { get; init; } = "GeneralHelp";
    public decimal Confidence { get; init; }
}
