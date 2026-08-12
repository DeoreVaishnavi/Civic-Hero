using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class ChatRepository : Repository<ChatSession>, IChatRepository
{
    public ChatRepository(CivicDbContext dbContext) : base(dbContext) { }

    public Task<ChatSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default) =>
        Query()
            .Include(entity => entity.Messages.OrderBy(message => message.SentAt))
            .FirstOrDefaultAsync(entity => entity.SessionId == sessionId, cancellationToken);
}
