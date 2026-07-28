using CivicHero.Backend.Core.DTOs.Disputes;

namespace CivicHero.Backend.Core.DTOs.Disputes
{
    public class InitiateDisputeDto
    {
        public int ComplaintId { get; set; }
        public int InitiatedByUserId { get; set; }
        public string? Details { get; set; }
    }
}