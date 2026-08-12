using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(CivicDbContext dbContext) : base(dbContext) { }

    public Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default) =>
        Query().CountAsync(entity => entity.UserId == userId && !entity.IsRead, cancellationToken);
}
