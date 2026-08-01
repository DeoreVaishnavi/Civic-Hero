using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class CreateComplaintRequest
{
    public long? DraftId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CitizenSeverity { get; set; } = "Medium";
    public long DepartmentId { get; set; }
    public long WardId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Landmark { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyReason { get; set; }

    // Evidence is the current multipart field and accepts supported images, video and documents.
    public List<IFormFile> Evidence { get; set; } = new();

    // Retained for compatibility with older clients that still post the original Images field.
    public List<IFormFile> Images { get; set; } = new();
}
