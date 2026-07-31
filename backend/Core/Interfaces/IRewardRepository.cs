using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public interface IRewardRepository : IRepository<RewardCatalog>
{
    Task<IReadOnlyList<RewardCatalog>> GetActiveCatalogAsync(CancellationToken cancellationToken = default);
}
