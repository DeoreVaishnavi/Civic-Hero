namespace CivicHero.Backend.Core.DTOs.Rewards
{
    public class RedemptionReceiptDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int RewardId { get; set; }
        public string? RewardTitle { get; set; }
        public int PointsSpent { get; set; }
        public DateTime RedeemedAt { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}
