using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RewardsController : ControllerBase
    {
        private readonly IRewardRepository _rewardRepository;
        private readonly IReputationService _reputationService;

        public RewardsController(
            IRewardRepository rewardRepository,
            IReputationService reputationService)
        {
            _rewardRepository = rewardRepository;
            _reputationService = reputationService;
        }

        // GET: api/rewards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RewardItemDto>>> GetAllRewards()
        {
            var rewards = await _rewardRepository.GetAllRewardsAsync();
            var rewardDtos = rewards.Select(r => new RewardItemDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description ?? string.Empty,
                PointsRequired = r.PointsRequired,
                QuantityAvailable = r.QuantityAvailable,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            return Ok(rewardDtos);
        }

        // GET: api/rewards/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RewardItemDto>> GetRewardById(int id)
        {
            var reward = await _rewardRepository.GetRewardByIdAsync(id);
            if (reward == null)
                return NotFound();

            var rewardDto = new RewardItemDto
            {
                Id = reward.Id,
                Title = reward.Title,
                Description = reward.Description ?? string.Empty,
                PointsRequired = reward.PointsRequired,
                QuantityAvailable = reward.QuantityAvailable,
                IsActive = reward.IsActive,
                CreatedAt = reward.CreatedAt,
                UpdatedAt = reward.UpdatedAt
            };

            return Ok(rewardDto);
        }

        // POST: api/rewards
        [HttpPost]
        public async Task<ActionResult<RewardItemDto>> CreateReward(RewardItemCreateDto rewardDto)
        {
            var reward = new RewardCatalog
            {
                Title = rewardDto.Title,
                Description = rewardDto.Description,
                PointsRequired = rewardDto.PointsRequired,
                QuantityAvailable = rewardDto.QuantityAvailable,
                IsActive = rewardDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdReward = await _rewardRepository.AddRewardAsync(reward);

            var createdDto = new RewardItemDto
            {
                Id = createdReward.Id,
                Title = createdReward.Title,
                Description = createdReward.Description ?? string.Empty,
                PointsRequired = createdReward.PointsRequired,
                QuantityAvailable = createdReward.QuantityAvailable,
                IsActive = createdReward.IsActive,
                CreatedAt = createdReward.CreatedAt,
                UpdatedAt = createdReward.UpdatedAt
            };

            return CreatedAtAction(nameof(GetRewardById), new { id = createdReward.Id }, createdDto);
        }

        // PUT: api/rewards/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<RewardItemDto>> UpdateReward(int id, RewardItemUpdateDto rewardDto)
        {
            var existingReward = await _rewardRepository.GetRewardByIdAsync(id);
            if (existingReward == null)
                return NotFound();

            if (rewardDto.Title != null)
                existingReward.Title = rewardDto.Title;
            if (rewardDto.Description != null)
                existingReward.Description = rewardDto.Description;
            if (rewardDto.PointsRequired.HasValue)
                existingReward.PointsRequired = rewardDto.PointsRequired.Value;
            if (rewardDto.QuantityAvailable.HasValue)
                existingReward.QuantityAvailable = rewardDto.QuantityAvailable.Value;
            if (rewardDto.IsActive.HasValue)
                existingReward.IsActive = rewardDto.IsActive.Value;

            existingReward.UpdatedAt = DateTime.UtcNow;

            var updatedReward = await _rewardRepository.UpdateRewardAsync(id, existingReward);

            if (updatedReward == null)
                return NotFound();

            var updatedDto = new RewardItemDto
            {
                Id = updatedReward.Id,
                Title = updatedReward.Title,
                Description = updatedReward.Description ?? string.Empty,
                PointsRequired = updatedReward.PointsRequired,
                QuantityAvailable = updatedReward.QuantityAvailable,
                IsActive = updatedReward.IsActive,
                CreatedAt = updatedReward.CreatedAt,
                UpdatedAt = updatedReward.UpdatedAt
            };

            return Ok(updatedDto);
        }

        // POST: api/rewards/redeem
        [HttpPost("redeem")]
        public async Task<ActionResult<RedemptionReceiptDto>> RedeemReward([FromBody] RedemptionRequest request)
        {
            // Validate request
            if (request == null || request.RewardId <= 0 || request.UserId <= 0)
                return BadRequest("Invalid request data.");

            // Get the reward
            var reward = await _rewardRepository.GetRewardByIdAsync(request.RewardId);
            if (reward == null)
                return NotFound("Reward not found.");

            if (!reward.IsActive)
                return BadRequest("Reward is not available for redemption.");

            if (reward.QuantityAvailable <= 0)
                return BadRequest("Reward is out of stock.");

            // Check if user has enough points
            var userPoints = await _reputationService.GetCurrentPointsAsync(request.UserId);
            if (userPoints < reward.PointsRequired)
                return BadRequest("Insufficient points for redemption.");

            // Process redemption
            var redemption = new Redemption
            {
                UserId = request.UserId,
                RewardId = reward.Id,
                PointsSpent = reward.PointsRequired,
                RedeemedAt = DateTime.UtcNow,
                Status = "Completed",
                Notes = request.Notes
            };

            var createdRedemption = await _rewardRepository.AddRedemptionAsync(redemption);

            // Deduct points from user
            await _reputationService.DeductPointsAsync(request.UserId, reward.PointsRequired, $"Redeemed reward: {reward.Title}");

            // Update reward quantity
            reward.QuantityAvailable -= 1;
            reward.UpdatedAt = DateTime.UtcNow;
            await _rewardRepository.UpdateRewardAsync(reward.Id, reward);

            // Create receipt DTO
            var receipt = new RedemptionReceiptDto
            {
                Id = createdRedemption.Id,
                UserId = createdRedemption.UserId,
                UserName = $"User {createdRedemption.UserId}", // In a real app, you'd get the actual username
                RewardId = createdRedemption.RewardId,
                RewardTitle = reward.Title,
                PointsSpent = createdRedemption.PointsSpent,
                RedeemedAt = createdRedemption.RedeemedAt,
                Status = createdRedemption.Status,
                Notes = createdRedemption.Notes
            };

            return Ok(receipt);
        }

        // GET: api/rewards/redemptions/user/{userId}
        [HttpGet("redemptions/user/{userId}")]
        public async Task<ActionResult<IEnumerable<RedemptionRowDto>>> GetUserRedemptions(int userId)
        {
            var redemptions = await _rewardRepository.GetRedemptionsByUserIdAsync(userId);
            var redemptionDtos = redemptions.Select(r => new RedemptionRowDto
            {
                Id = r.Id,
                UserName = $"User {r.UserId}", // In a real app, you'd get the actual username
                RewardTitle = r.Reward?.Title ?? "Unknown Reward",
                PointsSpent = r.PointsSpent,
                RedeemedAt = r.RedeemedAt,
                Status = r.Status
            }).ToList();

            return Ok(redemptionDtos);
        }

        // GET: api/rewards/leaderboard
        [HttpGet("leaderboard")]
        public async Task<ActionResult<IEnumerable<LeaderboardRowDto>>> GetLeaderboard([FromQuery] int count = 10)
        {
            var leaderboard = await _reputationService.GetLeaderboardAsync(count);
            return Ok(leaderboard);
        }
    }

    // Request model for redemption
    public class RedemptionRequest
    {
        public int UserId { get; set; }
        public int RewardId { get; set; }
        public string? Notes { get; set; }
    }
}