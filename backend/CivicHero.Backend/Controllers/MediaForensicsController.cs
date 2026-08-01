using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
public sealed class MediaForensicsController : ControllerBase
{
    private readonly IMediaForensicsService _service;

    public MediaForensicsController(IMediaForensicsService service) => _service = service;

    [HttpPost("complaints/{complaintId:long}/media-forensics/analyze")]
    public async Task<IActionResult> Analyze(
        long complaintId,
        CancellationToken cancellationToken) =>
        Ok(new
        {
            success = true,
            message = "Complaint media-forensics analysis completed.",
            data = await _service.AnalyzeComplaintMediaAsync(complaintId, cancellationToken)
        });
}
