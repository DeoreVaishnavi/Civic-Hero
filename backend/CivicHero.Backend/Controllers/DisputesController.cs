using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Disputes;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/disputes")]
[Authorize]
public sealed class DisputesController : ControllerBase
{
    private readonly IDisputeService _service;
    private readonly IServiceProvider _services;

    public DisputesController(IDisputeService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpGet("mine")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok(new { success = true, message = "Disputes loaded.", data = await _service.MineAsync(ct) });

    [HttpGet("officer")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    public async Task<IActionResult> Officer(CancellationToken ct) =>
        Ok(new { success = true, message = "Assigned dispute cases loaded.", data = await _service.OfficerMineAsync(ct) });

    [HttpGet("queue")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Queue([FromQuery] bool appealsOnly, CancellationToken ct) =>
        Ok(new { success = true, message = "Dispute queue loaded.", data = await _service.QueueAsync(appealsOnly, ct) });

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(new { success = true, message = "Dispute loaded.", data = await _service.GetAsync(id, ct) });

    [HttpGet("{id:long}/history")]
    public async Task<IActionResult> History(long id, CancellationToken ct) =>
        Ok(new { success = true, message = "Dispute decision history loaded.", data = await _service.HistoryAsync(id, ct) });

    [HttpPost("complaints/{complaintId:long}")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Raise(long complaintId, [FromBody] RaiseDisputeRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "Dispute raised.", data = await _service.RaiseAsync(complaintId, request, ct) });
    }

    [HttpPost("{id:long}/evidence")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> UploadEvidence(
        long id,
        [FromForm] List<IFormFile> evidence,
        [FromForm] long? requestId,
        CancellationToken ct) =>
        Ok(new { success = true, message = "Dispute evidence uploaded.", data = await _service.UploadEvidenceAsync(id, evidence, requestId, ct) });

    [HttpGet("{id:long}/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadEvidence(long id, long evidenceId, CancellationToken ct)
    {
        var download = await _service.DownloadEvidenceAsync(id, evidenceId, ct);
        return File(download.Content, download.ContentType, download.FileName, enableRangeProcessing: true);
    }

    [HttpPost("{id:long}/evidence-request")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> RequestEvidence(long id, [FromBody] DisputeEvidenceRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "Additional evidence requested.", data = await _service.RequestEvidenceAsync(id, request, ct) });
    }

    [HttpPost("{id:long}/supervisor-decision")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Supervisor(long id, [FromBody] DisputeDecisionRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "Supervisor decision saved.", data = await _service.SupervisorDecisionAsync(id, request, ct) });
    }

    [HttpPost("{id:long}/reopen-request")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Reopen(long id, [FromBody] ReopenDisputeRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "Reopen request submitted.", data = await _service.ReopenRequestAsync(id, request, ct) });
    }

    [HttpPost("{id:long}/appeal")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Appeal(long id, [FromBody] AppealDisputeRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "Appeal submitted.", data = await _service.AppealAsync(id, request, ct) });
    }

    [HttpPost("{id:long}/admin-decision")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Admin(long id, [FromBody] DisputeDecisionRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "Admin decision saved.", data = await _service.AdminDecisionAsync(id, request, ct) });
    }

    [HttpPost("{id:long}/superadmin-decision")]
    [Authorize(Policy = PermissionConstants.SuperAdminOnly)]
    public async Task<IActionResult> SuperAdmin(long id, [FromBody] DisputeDecisionRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new { success = true, message = "SuperAdmin final appeal decision saved.", data = await _service.SuperAdminDecisionAsync(id, request, ct) });
    }

    private async Task Validate<T>(T request, CancellationToken ct)
    {
        var validator = _services.GetService<IValidator<T>>();
        if (validator is null) return;
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(x => x.ErrorMessage));
    }
}
