using CivicHero.Backend.Core.DTOs.Chatbot;

namespace CivicHero.Backend.Core.Services;

public interface IChatbotService
{
    Task<ChatSessionResponse> StartSessionAsync(CancellationToken cancellationToken = default);
    Task<ChatSessionResponse> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatReplyResponse> SendMessageAsync(ChatMessageRequest request, CancellationToken cancellationToken = default);
    Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

public interface IPythonChatbotClient
{
    Task<PythonChatbotReply?> GenerateAsync(string message, string conversationId, long userId, CancellationToken cancellationToken = default);
}

public sealed record PythonChatbotReply(string Message, decimal Confidence);
