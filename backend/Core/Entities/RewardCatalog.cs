using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities
{
    public class RewardCatalog
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public int PointsRequired { get; set; }
        public int QuantityAvailable { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
    }
}
