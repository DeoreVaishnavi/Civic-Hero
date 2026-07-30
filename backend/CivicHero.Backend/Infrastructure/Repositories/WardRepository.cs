using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class WardRepository : Repository<Ward>, IWardRepository
{
    public WardRepository(CivicDbContext dbContext) : base(dbContext) { }

    public Task<Ward?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
}
