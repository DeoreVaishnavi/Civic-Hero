using CivicHero.Backend.Core.DTOs.Rewards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IReputationService
    {
        Task<int> GetCurrentPointsAsync(int userId);
        Task AddPointsAsync(int userId, int points, string reason);
        Task DeductPointsAsync(int userId, int points, string reason);
        Task<LeaderboardRowDto[]> GetLeaderboardAsync(int count = 10);
    }
}
