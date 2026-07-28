namespace CivicHero.Backend.Core.DTOs.Rewards
{
    public class RewardItemUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? PointsRequired { get; set; }
        public int? QuantityAvailable { get; set; }
        public bool? IsActive { get; set; }
    }
}
