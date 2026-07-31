namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintDetailResponse
{
    public ComplaintResponse Complaint { get; init; } = new();
    public string CitizenName { get; init; } = string.Empty;
    public string? AssignedOfficerName { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public IReadOnlyList<ComplaintImageDto> Images { get; init; } = Array.Empty<ComplaintImageDto>();
    public IReadOnlyList<ComplaintTimelineResponse> Timeline { get; init; } = Array.Empty<ComplaintTimelineResponse>();
}

public sealed class ComplaintImageDto
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public bool IsResolutionEvidence { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public string DownloadPath { get; init; } = string.Empty;
    public string? PublicUrl { get; init; }
}

