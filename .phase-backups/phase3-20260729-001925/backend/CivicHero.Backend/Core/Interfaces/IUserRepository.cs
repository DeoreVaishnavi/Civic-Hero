using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
