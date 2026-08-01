using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Verification;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/verification")]
[Authorize]
public sealed class VerificationController : ControllerBase
{
    private readonly IVerificationService _service;
    private readonly IServiceProvider _services;

    public VerificationController(IVerificationService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpGet("pending")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Pending(CancellationToken ct) =>
        Ok(new { success = true, message = "Pending verifications loaded.", data = await _service.GetPendingAsync(ct) });

    [HttpGet("queue")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Queue([FromQuery] bool overdueOnly, CancellationToken ct) =>
        Ok(new { success = true, message = "Verification queue loaded.", data = await _service.GetSupervisorQueueAsync(overdueOnly, ct) });

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken ct) =>
        Ok(new { success = true, message = "Verification history loaded.", data = await _service.GetHistoryAsync(ct) });

    [HttpGet("{complaintId:long}")]
    public async Task<IActionResult> Get(long complaintId, CancellationToken ct) =>
        Ok(new { success = true, message = "Verification loaded.", data = await _service.GetAsync(complaintId, ct) });

    [HttpPost("{complaintId:long}/geo-check")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Geo(long complaintId, [FromBody] GeoVerifyRequest request, CancellationToken ct) =>
        Ok(new { success = true, message = "Location checked.", data = await _service.CheckGeoAsync(complaintId, request, ct) });

    [HttpPost("{complaintId:long}/decision")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Decide(long complaintId, [FromBody] VerifyComplaintRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        var result = await _service.VerifyAsync(complaintId, request, ct);
        return Ok(new { success = true, message = DecisionMessage(result.Decision), data = result });
    }

    [HttpPut("{complaintId:long}/decision")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Amend(long complaintId, [FromBody] VerifyComplaintRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        var result = await _service.AmendAsync(complaintId, request, ct);
        return Ok(new { success = true, message = "Pending verification decision updated.", data = result });
    }

    [HttpDelete("{complaintId:long}/decision")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Withdraw(long complaintId, CancellationToken ct) =>
        Ok(new { success = true, message = "Pending verification decision withdrawn.", data = await _service.WithdrawAsync(complaintId, ct) });

    [HttpPost("{complaintId:long}/evidence")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    [RequestFormLimits(MultipartBodyLengthLimit = 26_214_400)]
    public async Task<IActionResult> UploadEvidence(long complaintId, [FromForm] VerificationEvidenceUploadRequest request, CancellationToken ct) =>
        Ok(new { success = true, message = "Citizen verification evidence uploaded.", data = await _service.UploadEvidenceAsync(complaintId, request, ct) });

    [HttpPost("{complaintId:long}/remind")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Remind(long complaintId, CancellationToken ct) =>
        Ok(new { success = true, message = "Reminder recorded.", data = await _service.RemindAsync(complaintId, ct) });

    [HttpPost("{complaintId:long}/supervisor-decision")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> SupervisorDecision(long complaintId, [FromBody] SupervisorVerificationDecisionRequest request, CancellationToken ct)
    {
        await Validate(request, ct);
        return Ok(new
        {
            success = true,
            message = request.ApproveCitizen
                ? "Citizen verification upheld and complaint returned for rework."
                : "Officer evidence accepted and complaint closed.",
            data = await _service.SupervisorDecisionAsync(complaintId, request, ct)
        });
    }

    private async Task Validate<T>(T request, CancellationToken ct)
    {
        var validator = _services.GetService<IValidator<T>>();
        if (validator is null) return;
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(x => x.ErrorMessage));
    }

    private static string DecisionMessage(string decision) => decision switch
    {
        "Approved" => "Resolution approved and complaint closed.",
        "NotResolvedYet" => "Issue marked not resolved and sent for supervisor review.",
        "RequestRevisit" => "Revisit requested and sent for supervisor review.",
        "PartiallyResolved" => "Issue marked partially resolved and sent for supervisor review.",
        _ => "Verification decision saved."
    };
}
