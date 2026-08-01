using CivicHero.Backend.Core.DTOs.Administration;

namespace CivicHero.Backend.Core.Services;

public interface ISuperAdminGovernanceService
{
    Task<IReadOnlyList<SuperAdminAdminAccountDto>> GetAdminAccountsAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<SuperAdminCreateAdminResponse> CreateAdminAsync(SuperAdminCreateAdminRequest request, CancellationToken cancellationToken = default);
    Task<SuperAdminAdminAccountDto> SetAdminActiveAsync(long userId, SuperAdminAdminAccountLifecycleRequest request, CancellationToken cancellationToken = default);
    Task<SuperAdminResetAdminPasswordResponse> ResetAdminPasswordAsync(long userId, SuperAdminResetAdminPasswordRequest request, CancellationToken cancellationToken = default);
    Task<SuperAdminGovernancePoliciesDto> GetPoliciesAsync(CancellationToken cancellationToken = default);
    Task<RolePolicyConfigurationDto> UpdateRolePolicyAsync(RolePolicyConfigurationDto request, CancellationToken cancellationToken = default);
    Task<AuthenticationPolicyDto> UpdateAuthenticationPolicyAsync(AuthenticationPolicyDto request, CancellationToken cancellationToken = default);
    Task<GlobalSessionListDto> GetGlobalSessionsAsync(string? search, string? role, CancellationToken cancellationToken = default);
    Task<GlobalSessionRevocationDto> RevokeGlobalSessionAsync(string sessionId, RevokeGlobalSessionRequest request, CancellationToken cancellationToken = default);
    Task<ReleaseGovernanceOverviewDto> GetReleaseDecisionsAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<ReleaseGovernanceDecisionDto> DecideReleaseAsync(string target, ReleaseGovernanceDecisionRequest request, CancellationToken cancellationToken = default);
}
