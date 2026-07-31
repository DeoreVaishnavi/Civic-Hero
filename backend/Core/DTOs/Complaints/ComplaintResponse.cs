namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintResponse
{
    public long Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public long DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public long WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string Address { get; init; } = string.Empty;
    public int ImageCount { get; init; }
    public int UpvoteCount { get; init; }
    public bool HasUpvoted { get; init; }
    public bool IsOwner { get; init; }
    public bool CanEdit { get; init; }
    public bool CanWithdraw { get; init; }
    public bool IsAnonymous { get; init; }
    public bool PossibleEmergency { get; init; }
    public string EmergencyReviewStatus { get; init; } = string.Empty;
    public double? DistanceKm { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
