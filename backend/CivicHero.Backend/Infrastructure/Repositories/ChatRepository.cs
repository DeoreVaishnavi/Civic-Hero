using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class ChatRepository : IChatRepository
{
    private readonly CivicDbContext _dbContext;

    public ChatRepository(CivicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ChatSession?> FindOwnedSessionAsync(
        string sessionId,
        long userId,
        bool tracking,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ChatSessions
            .Include(session => session.Messages)
            .Where(session => session.SessionId == sessionId && session.UserId == userId);

        if (!tracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> ListOwnedSessionsAsync(
        long userId,
        int take,
        CancellationToken cancellationToken = default) =>
        await _dbContext.ChatSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .Include(session => session.Messages)
            .OrderByDescending(session => session.StartedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public Task AddSessionAsync(ChatSession session, CancellationToken cancellationToken = default) =>
        _dbContext.ChatSessions.AddAsync(session, cancellationToken).AsTask();

    public Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default) =>
        _dbContext.ChatMessages.AddAsync(message, cancellationToken).AsTask();

    public void RemoveSession(ChatSession session) => _dbContext.ChatSessions.Remove(session);
}
