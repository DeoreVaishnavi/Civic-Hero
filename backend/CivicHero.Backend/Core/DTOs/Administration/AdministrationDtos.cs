namespace CivicHero.Backend.Core.DTOs.Administration;

public sealed record AdministrationOverviewDto(
    int ActiveDepartments,
    int ActiveWards,
    int ActiveCategories,
    int ActiveUsers,
    int OpenComplaints,
    int AuditEventsToday,
    DateTimeOffset GeneratedAt);

public sealed record CategoryDto(long Id, string Name, string Code, string? Description,
    string DefaultPriority, string? Icon, long? DepartmentId, string? DepartmentName,
    bool IsActive, int SortOrder, DateTimeOffset UpdatedAt);

public sealed class SaveCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultPriority { get; set; } = "Medium";
    public string? Icon { get; set; }
    public long? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed record DepartmentAdminDto(long Id, string Name, string Code, string? Description,
    bool IsActive, int WardCount, int ActiveUserCount, int OpenComplaintCount, DateTimeOffset UpdatedAt);

public sealed class SaveDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record WardAdminDto(long Id, long DepartmentId, string DepartmentName, string Name,
    string Code, decimal BoundaryNorth, decimal BoundarySouth, decimal BoundaryEast,
    decimal BoundaryWest, bool IsActive, int ActiveUserCount, int OpenComplaintCount, DateTimeOffset UpdatedAt);

public sealed class SaveWardRequest
{
    public long DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal BoundaryNorth { get; set; }
    public decimal BoundarySouth { get; set; }
    public decimal BoundaryEast { get; set; }
    public decimal BoundaryWest { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record SystemSettingDto(long Id, string Key, string Value, string ValueType,
    string? Description, string Group, bool IsPublic, bool IsSensitive,
    long? UpdatedByUserId, DateTimeOffset UpdatedAt);

public sealed class UpdateSystemSettingRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}

public sealed class AuditLogQuery
{
    public string? Search { get; set; }
    public string? IpAddress { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public string? UserRole { get; set; }
    public bool? Success { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed record AuditLogDto(long Id, long? UserId, string? UserEmail, string? UserRole,
    string Action, string EntityName, string? EntityId, string? IpAddress, string? UserAgent,
    string? CorrelationId, string Severity, bool Success, int HttpStatusCode,
    string? ErrorMessage, string? Changes, DateTimeOffset CreatedAt);

public sealed record AdminPagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record DependencyHealthDto(string Name, string Status, string? Description, double DurationMs);
public sealed record SystemHealthDto(string OverallStatus, string Environment, string Version,
    DateTimeOffset ServerTime, long ProcessUptimeSeconds, long ManagedMemoryMb,
    IReadOnlyList<DependencyHealthDto> Dependencies);

public sealed record MaintenancePreviewDto(int ExpiredReadNotifications, int ExpiredRefreshTokens,
    int ExpiredEmailVerificationTokens, int RetentionDays, bool DryRun, DateTimeOffset EvaluatedAt);
