using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Anonymous;

public sealed class CreateAnonymousComplaintRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public long WardId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string CaptchaToken { get; set; } = string.Empty;
    public bool ConsentToLimitedContactStorage { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyReason { get; set; }
    public List<IFormFile> Images { get; set; } = new();
}

public sealed class AnonymousComplaintCreatedResponse
{
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string TrackingToken { get; init; } = string.Empty;
    public DateTimeOffset TrackingExpiresAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}


public sealed class TrackAnonymousComplaintRequest
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string TrackingToken { get; set; } = string.Empty;
}

public sealed class AnonymousComplaintTrackingResponse
{
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public bool PossibleEmergency { get; init; }
    public string EmergencyReviewStatus { get; init; } = string.Empty;
    public string ContactHint { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public IReadOnlyList<AnonymousTimelineItem> Timeline { get; init; } = Array.Empty<AnonymousTimelineItem>();
}

public sealed class AnonymousTimelineItem
{
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
}
