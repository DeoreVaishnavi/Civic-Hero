using CivicHero.Backend.Core.DTOs.Comments;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/complaints/{complaintId:long}/comments")]
[Authorize]
public sealed class ComplaintCommentsController : ControllerBase
{
    private readonly IComplaintCommentService _service;
    public ComplaintCommentsController(IComplaintCommentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(long complaintId, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Comments loaded.", data = await _service.GetAsync(complaintId, cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Add(long complaintId, [FromBody] AddComplaintCommentRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, new { success = true, message = "Comment added.", data = await _service.AddAsync(complaintId, request, cancellationToken) });

    [HttpDelete("{commentId:long}")]
    public async Task<IActionResult> Delete(long complaintId, long commentId, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(complaintId, commentId, cancellationToken);
        return Ok(new { success = true, message = "Comment deleted." });
    }

    [HttpPost("{commentId:long}/moderate")]
    [Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
    public async Task<IActionResult> Moderate(long complaintId, long commentId, [FromBody] ModerateComplaintCommentRequest request, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Comment moderation updated.", data = await _service.ModerateAsync(complaintId, commentId, request, cancellationToken) });
}
