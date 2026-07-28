namespace CivicHero.Backend.Core.DTOs.Disputes
{
    public class ReAnalyzeRequest
    {
        public int ComplaintId { get; set; }
        public string Reason { get; set; } = string.Empty; // Reason for re-analysis request
    }
}
