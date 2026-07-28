using System;

namespace CivicHero.Backend.Core.DTOs.Complaints
{
    public class CreateComplaintDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsAnonymous { get; set; } = false;
        public int WardId { get; set; }
        public int? DepartmentId { get; set; }
    }
}