using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(CivicDbContext dbContext) : base(dbContext) { }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Query(asTracking: true)
            .FirstOrDefaultAsync(entity => entity.Email == email, cancellationToken);
}
