namespace CivicHero.Backend.Core.DTOs.Disputes
{
    public class VerdictSubmissionDto
    {
        public int DisputeId { get; set; }
        public string Verdict { get; set; } = string.Empty; // e.g., "Valid", "Invalid", "Partially Valid"
        public string Reasoning { get; set; } = string.Empty;
        public int? PointsAwarded { get; set; } // Points to award/deduct
        public int? PointsDeducted { get; set; }
    }
}
