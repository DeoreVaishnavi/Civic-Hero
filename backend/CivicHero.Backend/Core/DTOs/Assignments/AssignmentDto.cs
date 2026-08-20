namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class AssignmentDto
{
    public long AssignmentId { get; init; }
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ComplaintStatus { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public long DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public long WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public long CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public long OfficerId { get; init; }
    public string OfficerName { get; init; } = string.Empty;
    public string AssignmentStatus { get; init; } = string.Empty;
    public string? AssignmentReason { get; init; }
    public DateTimeOffset AssignedAt { get; init; }
    public DateTimeOffset AssignmentDueAt { get; init; }
    public DateTimeOffset ResolutionDueAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? EstimatedCompletionAt { get; init; }
    public string SlaState { get; init; } = string.Empty;
    public long RemainingMinutes { get; init; }
    public bool CanAccept { get; init; }
    public bool CanReject { get; init; }
    public bool CanAddProgress { get; init; }
    public bool CanComplete { get; init; }
    public bool CanReassign { get; init; }
    public bool CanRequestTransfer { get; init; }
    public bool CanRequestCitizenInformation { get; init; }
    public IReadOnlyList<ProgressUpdateDto> ProgressUpdates { get; init; } = Array.Empty<ProgressUpdateDto>();
    public IReadOnlyList<ResolutionEvidenceDto> ResolutionEvidence { get; init; } = Array.Empty<ResolutionEvidenceDto>();
    public IReadOnlyList<ProgressEvidenceDto> ProgressEvidence { get; init; } = Array.Empty<ProgressEvidenceDto>();
}

public sealed class ProgressUpdateDto
{
    public long Id { get; init; }
    public string OfficerName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ResolutionEvidenceDto
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public string DownloadPath { get; init; } = string.Empty;
}

public sealed class ProgressEvidenceDto
{
    public long Id { get; init; }
    public long ProgressUpdateId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public string DownloadPath { get; init; } = string.Empty;
    public string EvidenceType { get; init; } = string.Empty;
}
