namespace CivicHero.Backend.Core.DTOs.Comments;

public sealed class AddComplaintCommentRequest
{
    public string Body { get; set; } = string.Empty;
    public string Visibility { get; set; } = "Public";
}

public sealed class UpdateComplaintCommentRequest
{
    public string Body { get; set; } = string.Empty;
}

public sealed class ReportComplaintCommentRequest
{
    public string Category { get; set; } = "Unsafe";
    public string Reason { get; set; } = string.Empty;
}

public sealed class ComplaintCommentReportResponse
{
    public long CommentId { get; init; }
    public int ReportCount { get; init; }
    public bool ReportedByCurrentUser { get; init; }
    public string ModerationStatus { get; init; } = string.Empty;
    public bool AutomaticallyHidden { get; init; }
}

public sealed class ModerateComplaintCommentRequest
{
    public string Decision { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public sealed class ComplaintCommentResponse
{
    public long Id { get; init; }
    public long ComplaintId { get; init; }
    public long UserId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorRole { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Visibility { get; init; } = string.Empty;
    public string ModerationStatus { get; init; } = string.Empty;
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
    public bool CanReport { get; init; }
    public bool IsReportedByCurrentUser { get; init; }
    public int ReportCount { get; init; }
    public bool CanModerate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class ComplaintCommentModerationQuery
{
    public string? Status { get; set; } = "Hidden";
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}

public sealed class ComplaintCommentModerationItemResponse
{
    public long Id { get; init; }
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string ComplaintTitle { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorRole { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Visibility { get; init; } = string.Empty;
    public string ModerationStatus { get; init; } = string.Empty;
    public string? ModerationReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
