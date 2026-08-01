namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintPreSubmissionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
