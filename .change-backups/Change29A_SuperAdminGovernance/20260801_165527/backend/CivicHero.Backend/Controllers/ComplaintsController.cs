using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/complaints")]
[Authorize]
public sealed class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly IAiTriageService _aiTriageService;
    private readonly IValidator<CreateComplaintRequest> _createValidator;
    private readonly IValidator<UpdateComplaintRequest> _updateValidator;

    public ComplaintsController(
        IComplaintService complaintService,
        IAiTriageService aiTriageService,
        IValidator<CreateComplaintRequest> createValidator,
        IValidator<UpdateComplaintRequest> updateValidator)
    {
        _complaintService = complaintService;
        _aiTriageService = aiTriageService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateComplaintRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        var complaint = await _complaintService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = complaint.Complaint.Id }, new
        {
            success = true,
            message = "Complaint submitted successfully.",
            data = complaint
        });
    }

    [HttpPost("pre-submission-review")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> PreSubmissionReview([FromBody] ComplaintPreSubmissionRequest request, CancellationToken cancellationToken)
    {
        var title = (request.Title ?? string.Empty).Trim();
        var description = (request.Description ?? string.Empty).Trim();
        if (title.Length < 5 || description.Length < 20)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Enter a title of at least 5 characters and a description of at least 20 characters before requesting suggestions."]);
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Enter a valid location before requesting suggestions."]);

        var input = new AiTextRequest(title, description, request.Category?.Trim(), request.Latitude, request.Longitude, null);
        var classificationTask = _aiTriageService.ClassifyAsync(input, cancellationToken);
        var duplicateTask = _aiTriageService.CheckDuplicateAsync(input, null, cancellationToken);
        var priorityTask = _aiTriageService.PredictPriorityAsync(input, cancellationToken);
        await Task.WhenAll(classificationTask, duplicateTask, priorityTask);

        var classification = await classificationTask;
        var duplicate = await duplicateTask;
        var priority = await priorityTask;
        return OkEnvelope("Pre-submission suggestions completed.", new
        {
            classification,
            duplicate,
            priority,
            requiresManualReview = classification.Confidence < 0.70m || duplicate.Status != "Unique"
        });
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ComplaintQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Complaints loaded.", await _complaintService.GetAsync(query, cancellationToken));

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicFeed([FromQuery] ComplaintQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Public complaint feed loaded.", await _complaintService.GetPublicAsync(query, cancellationToken));

    [HttpGet("mine")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Mine([FromQuery] ComplaintQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Your complaints loaded.", await _complaintService.GetMineAsync(query, cancellationToken));

    [HttpGet("metadata")]
    [AllowAnonymous]
    public async Task<IActionResult> Metadata(CancellationToken cancellationToken) =>
        OkEnvelope("Complaint metadata loaded.", await _complaintService.GetMetadataAsync(cancellationToken));

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> Nearby([FromQuery] NearbyComplaintQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Nearby complaints loaded.", await _complaintService.GetNearbyAsync(query, cancellationToken));

    [HttpGet("stats/dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        OkEnvelope("Complaint dashboard loaded.", await _complaintService.GetDashboardAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Complaint loaded.", await _complaintService.GetByIdAsync(id, cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateComplaintRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        return OkEnvelope("Complaint updated.", await _complaintService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> DeleteBeforeAssignment(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Complaint deleted before assignment.", await _complaintService.WithdrawAsync(id, cancellationToken));

    [HttpPost("{id:long}/upvote")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> Upvote(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Upvote recorded.", new { upvoteCount = await _complaintService.UpvoteAsync(id, cancellationToken) });

    [HttpDelete("{id:long}/upvote")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> RemoveUpvote(long id, CancellationToken cancellationToken) =>
        OkEnvelope("Upvote removed.", new { upvoteCount = await _complaintService.RemoveUpvoteAsync(id, cancellationToken) });

    [HttpGet("{complaintId:long}/images/{imageId:long}")]
    public async Task<IActionResult> DownloadImage(long complaintId, long imageId, CancellationToken cancellationToken)
    {
        var download = await _complaintService.DownloadImageAsync(complaintId, imageId, cancellationToken);
        return File(download.Content, download.ContentType, download.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{complaintId:long}/images/{imageId:long}")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> RemoveEvidence(long complaintId, long imageId, CancellationToken cancellationToken) =>
        OkEnvelope("Complaint evidence removed.", await _complaintService.RemoveEvidenceAsync(complaintId, imageId, cancellationToken));

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(error => error.ErrorMessage));
    }
}
