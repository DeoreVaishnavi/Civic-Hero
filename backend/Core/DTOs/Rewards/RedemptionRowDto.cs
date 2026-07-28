namespace CivicHero.Backend.Core.DTOs.Rewards
{
    public class RedemptionRowDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? RewardTitle { get; set; }
        public int PointsSpent { get; set; }
        public DateTime RedeemedAt { get; set; }
        public string? Status { get; set; }
    }
}
