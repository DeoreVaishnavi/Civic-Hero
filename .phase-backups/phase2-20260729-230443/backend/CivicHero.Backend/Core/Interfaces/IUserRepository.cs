using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<User?> GetProfileByIdAsync(long id, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        UserQuery query,
        CancellationToken cancellationToken = default);
}
