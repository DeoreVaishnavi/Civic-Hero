using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services
{
    public class ReputationService : IReputationService
    {
        private readonly CivicHeroDbContext _context;

        public ReputationService(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetCurrentPointsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            return user.ReputationPoints;
        }

        public async Task AddPointsAsync(int userId, int points, string reason)
        {
            if (points <= 0)
            {
                throw new ArgumentException("Points must be positive.", nameof(points));
            }

            var log = new ReputationLog
            {
                UserId = userId,
                PointsChange = points,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            await _context.ReputationLogs.AddAsync(log);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            user.ReputationPoints += points;
            await _context.SaveChangesAsync();
        }

        public async Task DeductPointsAsync(int userId, int points, string reason)
        {
            if (points <= 0)
            {
                throw new ArgumentException("Points must be positive.", nameof(points));
            }

            var log = new ReputationLog
            {
                UserId = userId,
                PointsChange = -points,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            await _context.ReputationLogs.AddAsync(log);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            user.ReputationPoints -= points;
            await _context.SaveChangesAsync();
        }

        public async Task<LeaderboardRowDto[]> GetLeaderboardAsync(int count = 10)
        {
            var users = await _context.Users
                .OrderByDescending(u => u.ReputationPoints)
                .Take(count)
                .Select((u, index) => new LeaderboardRowDto
                {
                    UserId = u.Id,
                    UserName = u.FullName, // Using FullName as the display name
                    TotalPoints = u.ReputationPoints,
                    Rank = index + 1
                })
                .ToArrayAsync();

            return users;
        }
    }
}
