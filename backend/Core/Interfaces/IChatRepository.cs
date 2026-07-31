using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IChatRepository
{
    Task<ChatSession?> FindOwnedSessionAsync(
        string sessionId,
        long userId,
        bool tracking,
        CancellationToken cancellationToken = default);

    Task AddSessionAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
}
