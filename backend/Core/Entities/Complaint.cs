using CivicHero.Backend.Core.Enums;
using System;
using System.Collections.Generic;

namespace CivicHero.Backend.Core.Entities
{
    public class Complaint
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // e.g., Infrastructure, Sanitation, etc.
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = "Submitted"; // e.g., Submitted, InProgress, Resolved, Closed
        public int Priority { get; set; } = 1; // 1-Low, 2-Medium, 3-High, 4-Critical
        public bool IsAnonymous { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Foreign keys
        public int UserId { get; set; }
        public int WardId { get; set; }
        public int? DepartmentId { get; set; } // Optional, can be assigned later

        // Navigation properties
        public User? User { get; set; }
        public Ward? Ward { get; set; }
        public Department? Department { get; set; }
        public ICollection<ComplaintUpdate> Updates { get; set; } = new List<ComplaintUpdate>();
        public ICollection<DisputeAuditLog> DisputeAuditLogs { get; set; } = new List<DisputeAuditLog>();
        public ICollection<AiFraudAnalysis> AiFraudAnalyses { get; set; } = new List<AiFraudAnalysis>();
    }
}