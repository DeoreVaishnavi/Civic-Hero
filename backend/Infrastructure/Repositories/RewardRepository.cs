using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories
{
    public class RewardRepository : IRewardRepository
    {
        private readonly CivicHeroDbContext _context;

        public RewardRepository(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RewardCatalog>> GetAllRewardsAsync()
        {
            return await _context.RewardCatalogs
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<RewardCatalog?> GetRewardByIdAsync(int id)
        {
            return await _context.RewardCatalogs.FindAsync(id);
        }

        public async Task<RewardCatalog> AddRewardAsync(RewardCatalog reward)
        {
            _context.RewardCatalogs.Add(reward);
            await _context.SaveChangesAsync();
            return reward;
        }

        public async Task<RewardCatalog?> UpdateRewardAsync(int id, RewardCatalog reward)
        {
            var existing = await _context.RewardCatalogs.FindAsync(id);
            if (existing == null) return null;

            existing.Title = reward.Title;
            existing.Description = reward.Description;
            existing.PointsRequired = reward.PointsRequired;
            existing.QuantityAvailable = reward.QuantityAvailable;
            existing.IsActive = reward.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteRewardAsync(int id)
        {
            var reward = await _context.RewardCatalogs.FindAsync(id);
            if (reward == null) return false;

            _context.RewardCatalogs.Remove(reward);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Redemption>> GetRedemptionsByUserIdAsync(int userId)
        {
            return await _context.Redemptions
                .Where(r => r.UserId == userId)
                .Include(r => r.Reward)
                .ToListAsync();
        }

        public async Task<Redemption> AddRedemptionAsync(Redemption redemption)
        {
            _context.Redemptions.Add(redemption);
            await _context.SaveChangesAsync();
            return redemption;
        }

        public async Task<IEnumerable<Redemption>> GetAllRedemptionsAsync()
        {
            return await _context.Redemptions
                .Include(r => r.User)
                .Include(r => r.Reward)
                .ToListAsync();
        }
    }
}
