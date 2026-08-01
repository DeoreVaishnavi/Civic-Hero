namespace CivicHero.Backend.Core.DTOs.Chatbot;

public sealed class ChatSessionHistoryItemResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public DateTimeOffset LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public string Preview { get; init; } = string.Empty;
}

public sealed class RenameChatSessionRequest
{
    public string Title { get; set; } = string.Empty;
}

public sealed class ChatMessageFeedbackRequest
{
    public bool Helpful { get; set; }
    public string? Comment { get; set; }
}

public sealed class ChatMessageFeedbackResponse
{
    public long MessageId { get; init; }
    public string Feedback { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
}
