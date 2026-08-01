using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Assignments;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/assignments")]
[Authorize]
public sealed class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _service;
    private readonly IServiceProvider _services;

    public AssignmentsController(IAssignmentService service, IServiceProvider services)
    {
        _service = service;
        _services = services;
    }

    [HttpPost]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Assign([FromBody] AssignComplaintRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Complaint assigned.", await _service.AssignAsync(request, cancellationToken));
    }

    [HttpPost("bulk")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Bulk([FromBody] BulkAssignRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Complaints assigned.", await _service.BulkAssignAsync(request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/reassign")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Reassign(long complaintId, [FromBody] ReassignComplaintRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Complaint reassigned.", await _service.ReassignAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/accept")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    public async Task<IActionResult> Accept(long complaintId, CancellationToken cancellationToken) =>
        OkEnvelope("Assignment accepted.", await _service.AcceptAsync(complaintId, cancellationToken));

    [HttpPost("{complaintId:long}/reject")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    public async Task<IActionResult> Reject(long complaintId, [FromBody] RejectAssignmentRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Assignment rejected and returned to the supervisor queue.", await _service.RejectAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/progress")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    public async Task<IActionResult> Progress(long complaintId, [FromBody] AddProgressRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Progress update recorded.", await _service.AddProgressAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/complete")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Complete(long complaintId, [FromForm] CompleteAssignmentRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Resolution submitted for citizen verification.", await _service.CompleteAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/escalate")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Escalate(long complaintId, [FromBody] SupervisorActionRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Complaint escalated.", await _service.EscalateAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/resume")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Resume(long complaintId, [FromBody] SupervisorActionRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Escalated complaint returned to in-progress work.", await _service.ResumeEscalatedAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/request-rework")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> RequestRework(long complaintId, [FromBody] SupervisorActionRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Complaint returned for rework.", await _service.RequestReworkAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/instructions")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> SendInstruction(long complaintId, [FromBody] SupervisorMessageRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Instruction sent to the assigned officer.", await _service.SendOfficerInstructionAsync(complaintId, request, cancellationToken));
    }

    [HttpPost("{complaintId:long}/request-citizen-evidence")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> RequestCitizenEvidence(long complaintId, [FromBody] SupervisorMessageRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        return OkEnvelope("Additional evidence requested from the citizen.", await _service.RequestCitizenEvidenceAsync(complaintId, request, cancellationToken));
    }

    [HttpGet("escalation-history")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> EscalationHistory([FromQuery] AssignmentQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Escalation history loaded.", await _service.GetEscalationHistoryAsync(query, cancellationToken));

    [HttpGet("mine")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    public async Task<IActionResult> Mine([FromQuery] AssignmentQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Officer work queue loaded.", await _service.GetMyAssignmentsAsync(query, cancellationToken));

    [HttpGet("pending")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Pending([FromQuery] AssignmentQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Pending assignment queue loaded.", await _service.GetPendingAsync(query, cancellationToken));

    [HttpGet("overdue")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Overdue(CancellationToken cancellationToken) =>
        OkEnvelope("Overdue assignments loaded.", await _service.GetOverdueAsync(cancellationToken));

    [HttpGet("workload")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> Workload([FromQuery] long? departmentId, [FromQuery] long? wardId, CancellationToken cancellationToken) =>
        OkEnvelope("Officer workload loaded.", await _service.GetWorkloadAsync(departmentId, wardId, cancellationToken));

    [HttpGet("eligible-officers/{complaintId:long}")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> EligibleOfficers(long complaintId, CancellationToken cancellationToken) =>
        OkEnvelope("Eligible officers loaded.", await _service.GetEligibleOfficersAsync(complaintId, cancellationToken));

    [HttpGet("dashboard/officer")]
    [Authorize(Policy = PermissionConstants.OfficerOnly)]
    public async Task<IActionResult> OfficerDashboard(CancellationToken cancellationToken) =>
        OkEnvelope("Officer dashboard loaded.", await _service.GetOfficerDashboardAsync(cancellationToken));

    [HttpGet("dashboard/supervisor")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> SupervisorDashboard(CancellationToken cancellationToken) =>
        OkEnvelope("Supervisor dashboard loaded.", await _service.GetSupervisorDashboardAsync(cancellationToken));

    [HttpGet("{complaintId:long}")]
    public async Task<IActionResult> Get(long complaintId, CancellationToken cancellationToken) =>
        OkEnvelope("Assignment loaded.", await _service.GetByComplaintIdAsync(complaintId, cancellationToken));

    [HttpGet("{complaintId:long}/history")]
    public async Task<IActionResult> History(long complaintId, CancellationToken cancellationToken) =>
        OkEnvelope("Assignment history loaded.", await _service.GetHistoryAsync(complaintId, cancellationToken));

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });

    private async Task ValidateAsync<T>(T request, CancellationToken cancellationToken)
    {
        var validator = _services.GetService<IValidator<T>>();
        if (validator is null) return;
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(error => error.ErrorMessage));
    }
}
