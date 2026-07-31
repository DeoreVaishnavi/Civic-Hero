
using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/integration")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class IntegrationController : ControllerBase
{
    private readonly IIntegrationReadinessService _service;
    public IntegrationController(IIntegrationReadinessService service) => _service = service;
    [HttpGet("readiness")]
    public async Task<IActionResult> Readiness(CancellationToken cancellationToken) => OkEnvelope("Integration readiness loaded.", await _service.GetAsync(cancellationToken));
    [HttpPost("cache/test")]
    public async Task<IActionResult> Cache(CancellationToken cancellationToken) => OkEnvelope("Cache round-trip completed.", await _service.TestCacheAsync(cancellationToken));
    [HttpPost("messaging/test")]
    public async Task<IActionResult> Messaging(CancellationToken cancellationToken) => OkEnvelope("Messaging test completed.", await _service.TestMessagingAsync(Request.Headers["X-Correlation-ID"].FirstOrDefault(), cancellationToken));
    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
