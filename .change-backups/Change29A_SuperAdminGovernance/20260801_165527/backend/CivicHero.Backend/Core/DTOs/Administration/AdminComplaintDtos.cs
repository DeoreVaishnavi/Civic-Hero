using CivicHero.Backend.Core.DTOs.Common;

namespace CivicHero.Backend.Core.DTOs.Administration;

public sealed class AdminComplaintQuery
{
    private int _page = 1;
    private int _pageSize = 20;
    public int Page { get => _page; set => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 50); }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public bool IncludeArchived { get; set; }
    public bool DuplicateOnly { get; set; }
}

public sealed class AdminComplaintItemResponse
{
    public long Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public long DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public long WardId { get; init; }
    public string WardName { get; init; } = string.Empty;
    public long CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public string CitizenEmail { get; init; } = string.Empty;
    public long? AssignedOfficerId { get; init; }
    public string? AssignedOfficerName { get; init; }
    public long? DuplicateOfComplaintId { get; init; }
    public int MediaCount { get; init; }
    public bool IsArchived { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool CanClose { get; init; }
    public bool CanReopen { get; init; }
    public bool CanArchive { get; init; }
    public bool CanRestore { get; init; }
    public bool CanAssign { get; init; }
    public bool CanMerge { get; init; }
}

public sealed class AdminComplaintDetailResponse
{
    public AdminComplaintItemResponse Complaint { get; init; } = new();
    public IReadOnlyList<AdminComplaintMediaResponse> Media { get; init; } = [];
    public IReadOnlyList<AdminComplaintTimelineResponse> Timeline { get; init; } = [];
}

public sealed class AdminComplaintMediaResponse
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public bool IsResolutionEvidence { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public string DownloadPath { get; init; } = string.Empty;
}

public sealed class AdminComplaintTimelineResponse
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ActorName { get; init; } = "System";
    public DateTimeOffset Timestamp { get; init; }
}

public sealed class AdminComplaintPriorityRequest
{
    public string Priority { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminComplaintRoutingRequest
{
    public long DepartmentId { get; set; }
    public long WardId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminComplaintAssignmentRequest
{
    public long OfficerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminComplaintActionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminComplaintDuplicateRequest
{
    public long CanonicalComplaintId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class DuplicateClusterResponse
{
    public AdminComplaintItemResponse Canonical { get; init; } = new();
    public IReadOnlyList<AdminComplaintItemResponse> Duplicates { get; init; } = [];
    public int TotalSupportCount { get; init; }
}
