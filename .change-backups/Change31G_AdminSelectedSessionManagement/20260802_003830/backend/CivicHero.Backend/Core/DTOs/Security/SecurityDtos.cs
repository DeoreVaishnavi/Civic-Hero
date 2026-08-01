namespace CivicHero.Backend.Core.DTOs.Security;

public sealed record RateLimitMetricsDto(
    long AllowedRequests,
    long RejectedRequests,
    long AuthenticationRejections,
    long UploadRejections,
    long AdministrationRejections,
    DateTimeOffset StartedAtUtc);

public sealed record SecuritySessionDto(
    long UserId,
    string Email,
    string Role,
    bool IsEmailVerified,
    bool IsActive,
    bool HasRefreshSession,
    DateTimeOffset? RefreshSessionCreatedAtUtc,
    DateTimeOffset? RefreshSessionExpiresAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? TokenId,
    string? SessionId,
    int AuthorizationVersion,
    DateTimeOffset GeneratedAtUtc);

public sealed record SecurityOverviewDto(
    int ActiveUsers,
    int ActiveRefreshSessions,
    int LockedAccounts,
    int UsersWithFailedAttempts,
    int FailedWriteOperationsLast24Hours,
    int AuthenticationFailuresLast24Hours,
    int DistinctFailureAddressesLast24Hours,
    RateLimitMetricsDto RateLimits,
    DateTimeOffset GeneratedAtUtc);

public sealed record SecurityEventDto(
    long Id,
    long? UserId,
    string? UserEmail,
    string? UserRole,
    string Action,
    string EntityName,
    string? IpAddress,
    string Severity,
    bool Success,
    int HttpStatusCode,
    string? ErrorMessage,
    string? CorrelationId,
    DateTimeOffset CreatedAtUtc);

public sealed record LockedAccountDto(
    long Id,
    string FullName,
    string Email,
    string Role,
    int FailedLoginAttempts,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset? LastLoginAtUtc,
    bool IsActive);

public sealed record SecurityActionResultDto(
    long UserId,
    string Email,
    string Action,
    DateTimeOffset CompletedAtUtc);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);

public sealed record PersonalActivityDto(
    long Id,
    string Action,
    string EntityName,
    string? EntityId,
    string? IpAddress,
    string? UserAgent,
    string Severity,
    bool Success,
    int HttpStatusCode,
    string? ErrorMessage,
    string? CorrelationId,
    DateTimeOffset CreatedAtUtc);

public sealed record ActiveSessionDto(
    string SessionId,
    string SessionKind,
    string DeviceLabel,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastSeenAtUtc,
    bool IsCurrentSession,
    bool IsOnlyStoredSession);

public sealed record ActiveSessionsResponseDto(
    bool SupportsMultipleSessions,
    string ArchitectureNote,
    IReadOnlyList<ActiveSessionDto> Sessions,
    DateTimeOffset GeneratedAtUtc);
