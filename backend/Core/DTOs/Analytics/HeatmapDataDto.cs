namespace CivicHero.Backend.Core.DTOs.Analytics
{
    public class HeatmapDataDto
    {
        public int WardId { get; set; }
        public string WardName { get; set; } = string.Empty;
        public string WardCode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int IncidentCount { get; set; }
        public double RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }

        // For heatmap visualization - calculated intensity
        public double Intensity => IncidentCount > 0 ?
            Math.Min(1.0, Math.Log(1 + IncidentCount) / Math.Log(10)) : 0;
    }

    public enum RiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}