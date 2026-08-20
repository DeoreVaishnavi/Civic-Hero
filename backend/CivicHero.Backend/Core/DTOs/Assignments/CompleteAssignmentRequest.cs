using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class CompleteAssignmentRequest
{
    public string Notes { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public List<IFormFile> Evidence { get; set; } = new();
}
