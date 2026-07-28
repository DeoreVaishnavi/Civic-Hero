namespace CivicHero.Backend.Core.DTOs.Rewards
{
    public class LeaderboardRowDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int TotalPoints { get; set; } // or ReputationPoints
        public int Rank { get; set; }
    }
}
