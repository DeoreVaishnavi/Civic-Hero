using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/admin/complaints")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class AdminComplaintsController : ControllerBase
{
    private readonly IAdminComplaintService _service;
    public AdminComplaintsController(IAdminComplaintService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AdminComplaintQuery query, CancellationToken ct) =>
        OkEnvelope("Administrative complaint list loaded.", await _service.GetAsync(query, ct));

    [HttpGet("duplicate-clusters")]
    public async Task<IActionResult> DuplicateClusters([FromQuery] bool includeArchived, CancellationToken ct) =>
        OkEnvelope("Duplicate complaint clusters loaded.", await _service.GetDuplicateClustersAsync(includeArchived, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct) =>
        OkEnvelope("Administrative complaint detail loaded.", await _service.GetByIdAsync(id, ct));

    [HttpPut("{id:long}/priority")]
    public async Task<IActionResult> Priority(long id, [FromBody] AdminComplaintPriorityRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint priority updated.", await _service.ChangePriorityAsync(id, request, ct));

    [HttpPut("{id:long}/routing")]
    public async Task<IActionResult> Routing(long id, [FromBody] AdminComplaintRoutingRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint routing corrected.", await _service.CorrectRoutingAsync(id, request, ct));

    [HttpPost("{id:long}/assignment-override")]
    public async Task<IActionResult> AssignmentOverride(long id, [FromBody] AdminComplaintAssignmentRequest request, CancellationToken ct) =>
        OkEnvelope("Administrative assignment override completed.", await _service.OverrideAssignmentAsync(id, request, ct));

    [HttpPost("{id:long}/close")]
    public async Task<IActionResult> Close(long id, [FromBody] AdminComplaintActionRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint closed.", await _service.CloseAsync(id, request, ct));

    [HttpPost("{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long id, [FromBody] AdminComplaintActionRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint reopened for reassignment.", await _service.ReopenAsync(id, request, ct));

    [HttpPost("{id:long}/archive")]
    public async Task<IActionResult> Archive(long id, [FromBody] AdminComplaintActionRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint archived.", await _service.ArchiveAsync(id, request, ct));

    [HttpPost("{id:long}/restore")]
    public async Task<IActionResult> Restore(long id, [FromBody] AdminComplaintActionRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint restored.", await _service.RestoreAsync(id, request, ct));

    [HttpPost("{id:long}/link-duplicate")]
    public async Task<IActionResult> LinkDuplicate(long id, [FromBody] AdminComplaintDuplicateRequest request, CancellationToken ct) =>
        OkEnvelope("Duplicate complaint linked.", await _service.LinkDuplicateAsync(id, request, ct));

    [HttpPost("{id:long}/merge")]
    public async Task<IActionResult> Merge(long id, [FromBody] AdminComplaintDuplicateRequest request, CancellationToken ct) =>
        OkEnvelope("Duplicate complaint merged.", await _service.MergeAsync(id, request, ct));

    [HttpPost("{id:long}/media/{mediaId:long}/remove")]
    public async Task<IActionResult> RemoveMedia(long id, long mediaId, [FromBody] AdminComplaintActionRequest request, CancellationToken ct) =>
        OkEnvelope("Complaint media removed.", await _service.RemoveMediaAsync(id, mediaId, request, ct));

    private IActionResult OkEnvelope(string message, object data) => Ok(new { success = true, message, data });
}
