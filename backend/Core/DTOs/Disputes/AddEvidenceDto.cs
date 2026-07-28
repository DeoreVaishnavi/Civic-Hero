namespace CivicHero.Backend.Core.DTOs.Disputes
{
    public class AddEvidenceDto
    {
        public int ComplaintId { get; set; }
        public int UserId { get; set; }
        public string? EvidenceDescription { get; set; }
    }
}