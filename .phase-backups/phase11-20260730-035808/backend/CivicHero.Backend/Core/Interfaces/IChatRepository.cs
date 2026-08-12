using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IChatRepository : IRepository<ChatSession>
{
    Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
