namespace CivicHero.Backend.Core.DTOs.Emergency;

public sealed class EmergencyReviewQuery
{
    public string? Status { get; set; } = "Pending";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class EmergencyReviewDecisionRequest
{
    public string Decision { get; set; } = string.Empty;
    public string? ConfirmedPriority { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class EmergencyReviewResponse
{
    public long Id { get; init; }
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string ComplaintTitle { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ReporterReason { get; init; } = string.Empty;
    public string OriginalPriority { get; init; } = string.Empty;
    public string? ConfirmedPriority { get; init; }
    public string? ReviewedByName { get; init; }
    public string? DecisionReason { get; init; }
    public bool IsAnonymous { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
}
