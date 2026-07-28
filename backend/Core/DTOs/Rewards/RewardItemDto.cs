namespace CivicHero.Backend.Core.DTOs.Rewards
{
    public class RewardItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public int PointsRequired { get; set; }
        public int QuantityAvailable { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
