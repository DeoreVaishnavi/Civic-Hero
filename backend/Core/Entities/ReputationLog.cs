using System;

namespace CivicHero.Backend.Core.Entities
{
    public class ReputationLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PointsChange { get; set; } // positive for earning, negative for spending
        public string Reason { get; set; } = default!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }
}
