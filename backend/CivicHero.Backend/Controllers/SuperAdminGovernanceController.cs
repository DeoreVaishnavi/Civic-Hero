using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/superadmin/governance")]
[Authorize(Policy = PermissionConstants.SuperAdminOnly)]
public sealed class SuperAdminGovernanceController : ControllerBase
{
    private readonly ISuperAdminGovernanceService _service;
    public SuperAdminGovernanceController(ISuperAdminGovernanceService service) => _service = service;

    [HttpGet("admin-accounts")]
    public async Task<IActionResult> AdminAccounts([FromQuery] string? search, [FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        OkEnvelope("Admin accounts loaded.", await _service.GetAdminAccountsAsync(search, includeInactive, ct));

    [HttpPost("admin-accounts")]
    public async Task<IActionResult> CreateAdmin([FromBody] SuperAdminCreateAdminRequest request, CancellationToken ct) =>
        OkEnvelope("Admin account created.", await _service.CreateAdminAsync(request, ct));

    [HttpPut("admin-accounts/{userId:long}/active")]
    public async Task<IActionResult> SetAdminActive(long userId, [FromBody] SuperAdminAdminAccountLifecycleRequest request, CancellationToken ct) =>
        OkEnvelope($"Admin account {(request.IsActive ? "activated" : "deactivated")}.", await _service.SetAdminActiveAsync(userId, request, ct));

    [HttpPost("admin-accounts/{userId:long}/reset-password")]
    public async Task<IActionResult> ResetAdminPassword(long userId, [FromBody] SuperAdminResetAdminPasswordRequest request, CancellationToken ct) =>
        OkEnvelope("Admin temporary password reset and sessions revoked.", await _service.ResetAdminPasswordAsync(userId, request, ct));

    [HttpGet("policies")]
    public async Task<IActionResult> Policies(CancellationToken ct) =>
        OkEnvelope("Global governance policies loaded.", await _service.GetPoliciesAsync(ct));

    [HttpPut("policies/roles")]
    public async Task<IActionResult> UpdateRolePolicy([FromBody] RolePolicyConfigurationDto request, CancellationToken ct) =>
        OkEnvelope("Global deny-only role policy updated and active sessions revoked.", await _service.UpdateRolePolicyAsync(request, ct));

    [HttpPut("policies/authentication")]
    public async Task<IActionResult> UpdateAuthenticationPolicy([FromBody] AuthenticationPolicyDto request, CancellationToken ct) =>
        OkEnvelope("Global authentication policy updated and active sessions revoked.", await _service.UpdateAuthenticationPolicyAsync(request, ct));

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions([FromQuery] string? search, [FromQuery] string? role, CancellationToken ct) =>
        OkEnvelope("Global active sessions loaded.", await _service.GetGlobalSessionsAsync(search, role, ct));

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> RevokeSession(string sessionId, [FromBody] RevokeGlobalSessionRequest request, CancellationToken ct) =>
        OkEnvelope("Selected global session revoked.", await _service.RevokeGlobalSessionAsync(sessionId, request, ct));

    [HttpGet("release-decisions")]
    public async Task<IActionResult> ReleaseDecisions([FromQuery] int take = 100, CancellationToken ct = default) =>
        OkEnvelope("Release governance decisions loaded.", await _service.GetReleaseDecisionsAsync(take, ct));

    [HttpPost("release-decisions/{target}")]
    public async Task<IActionResult> DecideRelease(string target, [FromBody] ReleaseGovernanceDecisionRequest request, CancellationToken ct) =>
        OkEnvelope("Release governance decision recorded.", await _service.DecideReleaseAsync(target, request, ct));

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
