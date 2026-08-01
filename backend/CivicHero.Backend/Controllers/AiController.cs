using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly IAiTriageService _service;

    public AiController(IAiTriageService service) => _service = service;

    [HttpPost("classify")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Classify([FromBody] AiTextRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Complaint classification completed.", await _service.ClassifyAsync(request, cancellationToken));

    [HttpPost("duplicate-check")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DuplicateCheck([FromBody] AiTextRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Duplicate analysis completed.", await _service.CheckDuplicateAsync(request, null, cancellationToken));

    [HttpPost("fraud-check")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> FraudCheck([FromBody] AiTextRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Fraud analysis completed.", await _service.CheckFraudAsync(request, cancellationToken));

    [HttpPost("priority-predict")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Priority([FromBody] AiTextRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Priority prediction completed.", await _service.PredictPriorityAsync(request, cancellationToken));

    [HttpPost("complaints/{complaintId:long}/analyze")]
    [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
    public async Task<IActionResult> Analyze(long complaintId, [FromQuery] bool force = false, CancellationToken cancellationToken = default) =>
        OkEnvelope("Complaint AI triage completed.", await _service.AnalyzeComplaintAsync(complaintId, force, cancellationToken));

    [HttpGet("review-queue")]
    [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
    public async Task<IActionResult> ReviewQueue(CancellationToken cancellationToken) =>
        OkEnvelope("AI review queue loaded.", await _service.GetReviewQueueAsync(cancellationToken));

    [HttpPost("review-queue/{complaintId:long}/decision")]
    [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
    public async Task<IActionResult> Decide(long complaintId, [FromBody] AiReviewDecisionRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("AI review decision recorded.", await _service.DecideAsync(complaintId, request, cancellationToken));

    [HttpGet("hotspots")]
    [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
    public async Task<IActionResult> Hotspots([FromQuery] int days = 30, CancellationToken cancellationToken = default) =>
        OkEnvelope("AI hotspot analysis loaded.", await _service.GetHotspotsAsync(days, cancellationToken));

    [HttpGet("metrics")]
    [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken) =>
        OkEnvelope("AI metrics loaded.", await _service.GetMetricsAsync(cancellationToken));

    [HttpPost("retrain")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult Retrain([FromBody] RetrainRequest request) => Accepted(new
    {
        success = true,
        message = "Model retraining is an operational placeholder. Rule configuration can be versioned now; managed-model retraining belongs in the deployment pipeline.",
        data = new { status = "Accepted", request.Notes, requestedAt = DateTimeOffset.UtcNow }
    });

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
