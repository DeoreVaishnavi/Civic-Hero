using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class TransferAssignmentRequest
{
    public string ReasonCode { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class AddProgressEvidenceRequest
{
    public string Message { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset? EstimatedCompletionAt { get; set; }
    public List<IFormFile> Evidence { get; set; } = new();
}

public sealed class OfficerCitizenInformationRequest
{
    public string Message { get; set; } = string.Empty;
}

public sealed class OfficerPerformanceDto
{
    public int RangeDays { get; init; }
    public int TotalAssignments { get; init; }
    public int AcceptedAssignments { get; init; }
    public int CompletedAssignments { get; init; }
    public int ActiveAssignments { get; init; }
    public int TransferRequests { get; init; }
    public decimal AverageResponseMinutes { get; init; }
    public decimal AverageResolutionHours { get; init; }
    public decimal ClosureRatePercent { get; init; }
    public decimal VerificationSuccessRatePercent { get; init; }
    public decimal SlaCompliancePercent { get; init; }
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}

public sealed class OfficerMapItemDto
{
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string ComplaintStatus { get; init; } = string.Empty;
    public string AssignmentStatus { get; init; } = string.Empty;
    public string SlaState { get; init; } = string.Empty;
    public long RemainingMinutes { get; init; }
    public string Address { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public DateTimeOffset? EstimatedCompletionAt { get; init; }
}
