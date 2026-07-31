using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);
}
