using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class UpsertComplaintDraftRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? CitizenSeverity { get; set; }
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyReason { get; set; }
}

public sealed class AddComplaintDraftEvidenceRequest
{
    public IFormFile Evidence { get; set; } = null!;
}

public sealed record ComplaintDraftEvidenceResponse(
    long Id,
    string FileName,
    long FileSize,
    string MimeType,
    DateTimeOffset UploadedAt);

public sealed record ComplaintDraftResponse(
    long Id,
    string? Title,
    string? Description,
    string? Category,
    string CitizenSeverity,
    long? DepartmentId,
    long? WardId,
    decimal? Latitude,
    decimal? Longitude,
    string? Address,
    string? Landmark,
    bool PossibleEmergency,
    string? EmergencyReason,
    DateTimeOffset SavedAt,
    IReadOnlyList<ComplaintDraftEvidenceResponse> Evidence);
