using CivicHero.Backend.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IRewardRepository
    {
        Task<IEnumerable<RewardCatalog>> GetAllRewardsAsync();
        Task<RewardCatalog?> GetRewardByIdAsync(int id);
        Task<RewardCatalog> AddRewardAsync(RewardCatalog reward);
        Task<RewardCatalog?> UpdateRewardAsync(int id, RewardCatalog reward);
        Task<bool> DeleteRewardAsync(int id);
        Task<IEnumerable<Redemption>> GetRedemptionsByUserIdAsync(int userId);
        Task<Redemption> AddRedemptionAsync(Redemption redemption);
        Task<IEnumerable<Redemption>> GetAllRedemptionsAsync();
    }
}
