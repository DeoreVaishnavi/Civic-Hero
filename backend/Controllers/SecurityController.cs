using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/security")]
[Authorize]
public sealed class SecurityController : ControllerBase
{
    private readonly ISecurityService _service;
    public SecurityController(ISecurityService service) => _service = service;

    [HttpGet("session")]
    public async Task<IActionResult> Session(CancellationToken cancellationToken) =>
        OkEnvelope("Security session loaded.", await _service.GetMySessionAsync(cancellationToken));

    [HttpPost("sessions/revoke-all")]
    public async Task<IActionResult> RevokeMySessions(CancellationToken cancellationToken) =>
        OkEnvelope("All sessions were revoked. Sign in again to continue.", await _service.RevokeMySessionsAsync(cancellationToken));

    [HttpGet("admin/overview")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken) =>
        OkEnvelope("Security overview loaded.", await _service.GetOverviewAsync(cancellationToken));

    [HttpGet("admin/events")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Events([FromQuery] int take = 50, CancellationToken cancellationToken = default) =>
        OkEnvelope("Security events loaded.", await _service.GetEventsAsync(take, cancellationToken));

    [HttpGet("admin/locked-accounts")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> LockedAccounts(CancellationToken cancellationToken) =>
        OkEnvelope("Locked accounts loaded.", await _service.GetLockedAccountsAsync(cancellationToken));

    [HttpPost("admin/users/{id:long}/unlock")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Unlock(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Account unlocked.", await _service.UnlockAccountAsync(id, cancellationToken));

    [HttpPost("admin/users/{id:long}/revoke-sessions")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> RevokeUserSessions(long id, CancellationToken cancellationToken) =>
        OkEnvelope("User sessions revoked.", await _service.RevokeUserSessionsAsync(id, cancellationToken));

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
