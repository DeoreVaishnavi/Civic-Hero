using System;

namespace CivicHero.Backend.Core.DTOs.Complaints
{
    public class UpdateComplaintDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Status { get; set; }
        public int? Priority { get; set; }
        public bool? IsAnonymous { get; set; }
        public int? WardId { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}