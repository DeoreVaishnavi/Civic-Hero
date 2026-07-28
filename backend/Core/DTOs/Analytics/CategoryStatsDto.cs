namespace CivicHero.Backend.Core.DTOs.Analytics
{
    public class CategoryStatsDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}