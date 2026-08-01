using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/complaint-drafts")]
[Authorize(Policy = PermissionConstants.CitizenOnly)]
public sealed class ComplaintDraftsController : ControllerBase
{
    private readonly IComplaintDraftService _drafts;
    private readonly IValidator<UpsertComplaintDraftRequest> _saveValidator;
    private readonly IValidator<AddComplaintDraftEvidenceRequest> _evidenceValidator;

    public ComplaintDraftsController(
        IComplaintDraftService drafts,
        IValidator<UpsertComplaintDraftRequest> saveValidator,
        IValidator<AddComplaintDraftEvidenceRequest> evidenceValidator)
    {
        _drafts = drafts;
        _saveValidator = saveValidator;
        _evidenceValidator = evidenceValidator;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken) =>
        OkEnvelope("Complaint draft loaded.", await _drafts.GetCurrentAsync(cancellationToken));

    [HttpPut("current")]
    public async Task<IActionResult> SaveCurrent([FromBody] UpsertComplaintDraftRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_saveValidator, request, cancellationToken);
        return OkEnvelope("Complaint draft saved to your account.", await _drafts.SaveCurrentAsync(request, cancellationToken));
    }

    [HttpPost("current/evidence")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<IActionResult> AddEvidence([FromForm] AddComplaintDraftEvidenceRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_evidenceValidator, request, cancellationToken);
        return OkEnvelope("Draft evidence saved.", await _drafts.AddEvidenceAsync(request.Evidence, cancellationToken));
    }

    [HttpGet("current/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadEvidence(long evidenceId, CancellationToken cancellationToken)
    {
        var download = await _drafts.DownloadEvidenceAsync(evidenceId, cancellationToken);
        return File(download.Content, download.ContentType, download.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("current/evidence/{evidenceId:long}")]
    public async Task<IActionResult> RemoveEvidence(long evidenceId, CancellationToken cancellationToken)
    {
        await _drafts.RemoveEvidenceAsync(evidenceId, cancellationToken);
        return OkEnvelope("Draft evidence removed.", new { evidenceId });
    }

    [HttpDelete("current")]
    public async Task<IActionResult> DeleteCurrent(CancellationToken cancellationToken)
    {
        await _drafts.DeleteCurrentAsync(cancellationToken);
        return OkEnvelope("Complaint draft cleared.", new { deleted = true });
    }

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(error => error.ErrorMessage));
    }
}
