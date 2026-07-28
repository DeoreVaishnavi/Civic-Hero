using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities
{
    public class AiFraudAnalysis
    {
        public int id { get; set; }
        public int ComplaintId { get; set; }
        public int FraudScore { get; set; }
        public FraudRiskLevel RiskLevel { get; set; }
        public string Reasons { get; set; } = string.Empty;
        public bool Reviewed { get; set; }
        public DateTime CreatedAt { get; set; }
        //public Complaint Complaint { get; set; }

    }
}
