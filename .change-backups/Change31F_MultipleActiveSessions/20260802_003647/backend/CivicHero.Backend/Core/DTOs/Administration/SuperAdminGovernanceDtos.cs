namespace CivicHero.Backend.Core.DTOs.Administration;

public sealed class SuperAdminCreateAdminRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? TemporaryPassword { get; set; }
    public bool MarkEmailVerified { get; set; } = true;
}

public sealed class SuperAdminAdminAccountLifecycleRequest
{
    public bool IsActive { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class SuperAdminResetAdminPasswordRequest
{
    public string? TemporaryPassword { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record SuperAdminAdminAccountDto(
    long Id,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    bool IsEmailVerified,
    bool TwoFactorEnabled,
    DateTimeOffset? TwoFactorEnabledAt,
    DateTimeOffset? LastLoginAt,
    bool HasActiveSession,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SuperAdminCreateAdminResponse(
    SuperAdminAdminAccountDto Account,
    string TemporaryPassword,
    bool RequiresEmailVerification,
    DateTimeOffset? VerificationExpiresAtUtc,
    string? DevelopmentVerificationToken,
    DateTimeOffset? TwoFactorEnrollmentDeadlineUtc);

public sealed record SuperAdminResetAdminPasswordResponse(
    long UserId,
    string Email,
    string TemporaryPassword,
    DateTimeOffset ChangedAtUtc);

public sealed class RolePolicyEntryDto
{
    public string Role { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public IReadOnlyList<string> DeniedApiPrefixes { get; set; } = Array.Empty<string>();
}

public sealed class RolePolicyConfigurationDto
{
    public IReadOnlyList<RolePolicyEntryDto> Roles { get; set; } = Array.Empty<RolePolicyEntryDto>();
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long? UpdatedByUserId { get; set; }
    public string EnforcementNote { get; set; } = "Role policy can disable a role or add deny-only API prefixes. It never grants access beyond controller authorization policies.";
}

public sealed class AuthenticationPolicyDto
{
    public int MaximumFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 30;
    public bool RequireVerifiedEmail { get; set; } = true;
    public IReadOnlyList<string> TwoFactorRequiredRoles { get; set; } = Array.Empty<string>();
    public int TwoFactorEnrollmentGraceHours { get; set; } = 24;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long? UpdatedByUserId { get; set; }
}

public sealed record SuperAdminGovernancePoliciesDto(
    RolePolicyConfigurationDto RolePolicy,
    AuthenticationPolicyDto AuthenticationPolicy);

public sealed class TwoFactorEnrollmentExemptionDto
{
    public long UserId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public sealed record GlobalSessionDto(
    string SessionId,
    long UserId,
    string FullName,
    string Email,
    string Role,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    bool TwoFactorEnabled);

public sealed record GlobalSessionListDto(
    bool SupportsMultipleSessionsPerUser,
    string SchemaNote,
    IReadOnlyList<GlobalSessionDto> Sessions,
    DateTimeOffset GeneratedAtUtc);

public sealed class RevokeGlobalSessionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed record GlobalSessionRevocationDto(
    string SessionId,
    long UserId,
    string Email,
    string Action,
    DateTimeOffset RevokedAtUtc);

public sealed class ReleaseGovernanceDecisionRequest
{
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ReleaseVersion { get; set; }
    public string? EvidenceReference { get; set; }
}

public sealed record ReleaseGovernanceDecisionDto(
    string Target,
    string Decision,
    string Reason,
    string? ReleaseVersion,
    string? EvidenceReference,
    long DecidedByUserId,
    string? DecidedByEmail,
    DateTimeOffset DecidedAtUtc,
    bool EvidenceValidated);

public sealed record ReleaseGovernanceHistoryItemDto(
    long Id,
    string Target,
    string Decision,
    string Reason,
    string? ReleaseVersion,
    string? EvidenceReference,
    long? DecidedByUserId,
    string? DecidedByEmail,
    DateTimeOffset DecidedAtUtc,
    bool EvidenceValidated);

public sealed record ReleaseGovernanceOverviewDto(
    IReadOnlyList<ReleaseGovernanceDecisionDto> CurrentDecisions,
    IReadOnlyList<ReleaseGovernanceHistoryItemDto> History,
    DateTimeOffset GeneratedAtUtc);
