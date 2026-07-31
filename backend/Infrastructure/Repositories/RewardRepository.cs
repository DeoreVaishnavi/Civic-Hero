using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class RewardRepository : Repository<RewardCatalog>, IRewardRepository
{
    public RewardRepository(CivicDbContext dbContext) : base(dbContext) { }

    public async Task<IReadOnlyList<RewardCatalog>> GetActiveCatalogAsync(
        CancellationToken cancellationToken = default) =>
        await Query()
            .Where(entity => entity.IsActive && entity.StockQuantity > 0)
            .OrderBy(entity => entity.PointsCost)
            .ToListAsync(cancellationToken);
}
