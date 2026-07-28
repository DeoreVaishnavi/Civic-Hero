using System;

namespace CivicHero.Backend.Core.Entities
{
    public class ComplaintUpdate
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string UpdateType { get; set; } = string.Empty; // e.g., StatusChange, Comment, Assignment
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int UserId { get; set; } // User who made the update

        // Navigation properties
        public Complaint? Complaint { get; set; }
        public User? User { get; set; }
    }
}