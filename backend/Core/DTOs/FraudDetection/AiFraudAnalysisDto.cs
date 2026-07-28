using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.FraudDetection
{
    public class AiFraudAnalysisDto
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public int FraudScore { get; set; } // 0-100 scale
        public FraudRiskLevel RiskLevel { get; set; } // Low, Medium, High, Critical
        public string Reasons { get; set; } = string.Empty; // Comma-separated reasons or JSON
        public bool Reviewed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
