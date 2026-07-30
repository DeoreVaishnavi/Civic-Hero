namespace CivicHero.Backend.Core.DTOs.AI;

/// <summary>
/// Request for comparing a complaint image with a resolution image to verify if the issue has been resolved.
/// </summary>
public class VisionComparisonRequest
{
    public int ComplaintId { get; set; }
    public byte[]? ComplaintImageBytes { get; set; }
    public byte[]? ResolutionImageBytes { get; set; }
    public double? ComplaintLatitude { get; set; }
    public double? ComplaintLongitude { get; set; }
    public DateTime? IncidentTime { get; set; }
    public string? ComplaintDescription { get; set; }
}