using System;

namespace CivicHero.Backend.Core.DTOs.Complaints
{
    public class CreateComplaintUpdateDto
    {
        public string UpdateType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}