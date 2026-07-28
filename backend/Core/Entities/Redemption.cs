using System;

namespace CivicHero.Backend.Core.Entities
{
    public class Redemption
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RewardId { get; set; }
        public int PointsSpent { get; set; }
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed"; // e.g., Pending, Completed, Cancelled
        public string? Notes { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public RewardCatalog? Reward { get; set; }
    }
}
