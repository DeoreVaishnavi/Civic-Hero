using CivicHero.Backend.Core.DTOs.Chatbot;

namespace CivicHero.Backend.Core.Services;

public interface IChatbotService
{
    Task<ChatSessionResponse> StartSessionAsync(CancellationToken cancellationToken = default);
    Task<ChatSessionResponse> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSessionHistoryItemResponse>> ListSessionsAsync(string? search, int take, CancellationToken cancellationToken = default);
    Task<ChatSessionResponse> RenameSessionAsync(string sessionId, RenameChatSessionRequest request, CancellationToken cancellationToken = default);
    Task<ChatSessionResponse> ContinueSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatReplyResponse> SendMessageAsync(ChatMessageRequest request, CancellationToken cancellationToken = default);
    Task<ChatMessageFeedbackResponse> SubmitFeedbackAsync(string sessionId, long messageId, ChatMessageFeedbackRequest request, CancellationToken cancellationToken = default);
    Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
