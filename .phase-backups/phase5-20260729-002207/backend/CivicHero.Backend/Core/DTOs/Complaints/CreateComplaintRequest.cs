using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class CreateComplaintRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public long WardId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public List<IFormFile> Images { get; set; } = new();
}
