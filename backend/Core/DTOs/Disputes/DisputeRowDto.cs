namespace CivicHero.Backend.Core.DTOs.Disputes
{
    public class DisputeRowDto
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string? ComplainantName { get; set; }
        public string? ComplainantEmail { get; set; }
        public DateTime DisputeInitiatedAt { get; set; }
        public string? CurrentStatus { get; set; } // e.g., "Pending", "Under Review", "Resolved"
        public int? InitiatedByUserId { get; set; }
    }
}
