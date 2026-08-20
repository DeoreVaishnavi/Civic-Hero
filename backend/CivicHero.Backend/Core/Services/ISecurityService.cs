using CivicHero.Backend.Core.DTOs.Security;

namespace CivicHero.Backend.Core.Services;

public interface IRateLimitMonitor
{
    void RecordAllowed(string policy);
    void RecordRejected(string policy);
    RateLimitMetricsDto Snapshot();
}

public interface ISecurityService
{
    Task<SecuritySessionDto> GetMySessionAsync(CancellationToken cancellationToken = default);
    Task<SecurityActionResultDto> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonalActivityDto>> GetMyActivityAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<ActiveSessionsResponseDto> GetMySessionsAsync(CancellationToken cancellationToken = default);
    Task<SecurityActionResultDto> RevokeMySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<SecurityActionResultDto> RevokeMySessionsAsync(CancellationToken cancellationToken = default);
    Task<SecurityOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEventDto>> GetEventsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LockedAccountDto>> GetLockedAccountsAsync(CancellationToken cancellationToken = default);
    Task<SecurityActionResultDto> UnlockAccountAsync(long userId, CancellationToken cancellationToken = default);
    Task<SecurityActionResultDto> RevokeUserSessionsAsync(long userId, CancellationToken cancellationToken = default);
    Task<ManagedSessionsResponseDto> GetManagedSessionsAsync(string? search, string? role, int take = 100, CancellationToken cancellationToken = default);
    Task<ManagedSessionRevocationDto> RevokeManagedSessionAsync(string sessionId, RevokeManagedSessionRequest request, CancellationToken cancellationToken = default);
}
