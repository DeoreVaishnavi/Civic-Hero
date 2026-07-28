using System;

namespace CivicHero.Backend.Core.DTOs.Complaints
{
    public class ComplaintUpdateDto
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string UpdateType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
    }
}