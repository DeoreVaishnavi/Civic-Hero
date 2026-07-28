using System;

namespace CivicHero.Backend.Core.Entities
{
    public class DisputeAuditLog
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public int InitiatedByUserId { get; set; }
        public string Action { get; set; } = string.Empty; // e.g., "Dispute initiated", "Evidence submitted", "Verdict submitted"
        public string Details { get; set; } = string.Empty; // JSON or text details
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User? User { get; set; }
        // public Complaint? Complaint { get; set; } // Assuming Complaint entity exists
    }
}
