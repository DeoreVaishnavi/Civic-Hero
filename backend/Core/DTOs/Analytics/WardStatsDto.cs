using System.Collections.Generic;

namespace CivicHero.Backend.Core.DTOs.Analytics
{
    public class WardStatsDto
    {
        public int WardId { get; set; }
        public string WardName { get; set; } = string.Empty;
        public string WardCode { get; set; } = string.Empty;
        public int TotalReports { get; set; }
        public int ResolvedReports { get; set; }
        public double ResolutionRate { get; set; }
        public List<double> HeatmapData { get; set; } = new List<double>();
    }
}