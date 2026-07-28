namespace CivicHero.Backend.Core.DTOs.Rewards
{
    public class RewardItemCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PointsRequired { get; set; }
        public int QuantityAvailable { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
