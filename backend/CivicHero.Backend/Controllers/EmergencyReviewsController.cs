using CivicHero.Backend.Core.DTOs.Emergency;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/emergency-reviews")]
[Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
public sealed class EmergencyReviewsController : ControllerBase
{
    private readonly IEmergencyReviewService _service;
    public EmergencyReviewsController(IEmergencyReviewService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] EmergencyReviewQuery query, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Emergency review queue loaded.", data = await _service.GetAsync(query, cancellationToken) });

    [HttpPost("{reviewId:long}/decision")]
    public async Task<IActionResult> Decide(long reviewId, [FromBody] EmergencyReviewDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Emergency review decision recorded.", data = await _service.DecideAsync(reviewId, request, cancellationToken) });
}
