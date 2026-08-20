using CivicHero.Backend.Core.DTOs.VisualVerification;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/visual-verification")]
[Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
public sealed class VisualVerificationController : ControllerBase
{
    private readonly IVisualVerificationService _service;
    public VisualVerificationController(IVisualVerificationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Queue([FromQuery] VisualVerificationQuery query, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Visual verification queue loaded.", data = await _service.GetQueueAsync(query, cancellationToken) });

    [HttpGet("{analysisId:long}")]
    public async Task<IActionResult> Get(long analysisId, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Visual verification analysis loaded.", data = await _service.GetByIdAsync(analysisId, cancellationToken) });

    [HttpPost("complaints/{complaintId:long}/analyze")]
    public async Task<IActionResult> Analyze(long complaintId, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Visual verification completed.", data = await _service.AnalyzeComplaintAsync(complaintId, cancellationToken) });

    [HttpPost("{analysisId:long}/review")]
    public async Task<IActionResult> Review(long analysisId, [FromBody] VisualVerificationHumanDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Human visual review recorded.", data = await _service.ReviewAsync(analysisId, request, cancellationToken) });
}
