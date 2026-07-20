using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Repository contract for reward catalog operations.
/// </summary>
public interface IRewardRepository : IRepository<RewardCatalog>
{
    /// <summary>
    /// Gets all active rewards.
    /// </summary>
    Task<IReadOnlyList<RewardCatalog>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rewards by reward type.
    /// </summary>
    Task<IReadOnlyList<RewardCatalog>> GetByTypeAsync(
        RewardType rewardType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rewards that can be redeemed using
    /// the specified number of points.
    /// </summary>
    Task<IReadOnlyList<RewardCatalog>> GetAvailableForPointsAsync(
        int availablePoints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rewards whose required points fall
    /// within the specified range.
    /// </summary>
    Task<IReadOnlyList<RewardCatalog>> GetByPointRangeAsync(
        int minimumPoints,
        int maximumPoints,
        CancellationToken cancellationToken = default);
}