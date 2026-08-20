using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Administration;

public sealed class AdminUserManagementQuery
{
    private int _page = 1;
    private int _pageSize = 20;
    public int Page { get => _page; set => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 100); }
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
}

public sealed record AdminManagedUserDto(
    long Id,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    long? DepartmentId,
    string? DepartmentName,
    long? WardId,
    string? WardName,
    bool IsActive,
    bool IsEmailVerified,
    bool IsPhoneVerified,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class AdminCreateCitizenRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? TemporaryPassword { get; set; }
    public bool MarkEmailVerified { get; set; } = true;
}

public sealed record AdminCreateCitizenResponse(
    AdminManagedUserDto User,
    string TemporaryPassword,
    bool RequiresEmailVerification,
    DateTimeOffset? VerificationExpiresAtUtc,
    string? DevelopmentVerificationToken);

public sealed class AdminUpdateUserEmailRequest
{
    public string NewEmail { get; set; } = string.Empty;
    public bool MarkVerified { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record AdminUpdateUserEmailResponse(
    AdminManagedUserDto User,
    bool RequiresEmailVerification,
    DateTimeOffset? VerificationExpiresAtUtc,
    string? DevelopmentVerificationToken);

public sealed class AdminUserLifecycleRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool ActivateOnRestore { get; set; } = true;
}

public sealed record AdminUserHistoryItemDto(
    long Id,
    string Action,
    string Category,
    long? ActorUserId,
    string? ActorEmail,
    string? ActorRole,
    string? OldValuesJson,
    string? NewValuesJson,
    bool Success,
    int HttpStatusCode,
    DateTimeOffset CreatedAt);

public sealed record AdminUserHistoryResponse(
    AdminManagedUserDto User,
    IReadOnlyList<AdminUserHistoryItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class DepartmentHeadAssignmentRequest
{
    public long UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class DepartmentHeadRemovalRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed record DepartmentHeadDto(
    long DepartmentId,
    string DepartmentName,
    string DepartmentCode,
    long? UserId,
    string? FullName,
    string? Email,
    string? Role,
    DateTimeOffset? AssignedAt,
    long? AssignedByUserId);

public sealed class PriorityRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public int AssignmentHours { get; set; }
    public int ResolutionHours { get; set; }
}

public sealed class StatusRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool CitizenVisible { get; set; } = true;
    public bool IsTerminal { get; set; }
    public int SortOrder { get; set; }
}

public sealed class VerificationPolicyDto
{
    public int MediumWindowHours { get; set; } = 72;
    public int HighWindowHours { get; set; } = 48;
    public int CriticalWindowHours { get; set; } = 24;
    public int AppealGraceDays { get; set; } = 7;
    public int GeoFenceMeters { get; set; } = 500;
}

public sealed class EscalationPolicyDto
{
    public int AtRiskPercent { get; set; } = 80;
    public int EscalationGraceHours { get; set; } = 12;
    public int MaximumAutomaticEscalations { get; set; } = 3;
    public bool NotifySupervisor { get; set; } = true;
    public bool NotifyAdminAfterRepeat { get; set; } = true;
}

public sealed class FeatureFlagDto
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string Audience { get; set; } = "All";
}

public sealed class AuthorityDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AdminMasterDataConfigurationDto
{
    public IReadOnlyList<PriorityRuleDto> Priorities { get; set; } = Array.Empty<PriorityRuleDto>();
    public IReadOnlyList<StatusRuleDto> Statuses { get; set; } = Array.Empty<StatusRuleDto>();
    public VerificationPolicyDto Verification { get; set; } = new();
    public EscalationPolicyDto Escalation { get; set; } = new();
    public IReadOnlyList<FeatureFlagDto> FeatureFlags { get; set; } = Array.Empty<FeatureFlagDto>();
    public IReadOnlyList<AuthorityDto> Authorities { get; set; } = Array.Empty<AuthorityDto>();
    public DateTimeOffset UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
}
