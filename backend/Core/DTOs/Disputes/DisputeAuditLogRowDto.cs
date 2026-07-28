namespace CivicHero.Backend.Core.DTOs.Disputes
{
    public class DisputeAuditLogRowDto
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
