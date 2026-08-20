using CivicHero.Backend.Core.DTOs.Comments;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/comment-moderation")]
[Authorize(Roles = "Supervisor,Admin,SuperAdmin")]
public sealed class CommentModerationController : ControllerBase
{
    private readonly IComplaintCommentService _service;
    public CommentModerationController(IComplaintCommentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ComplaintCommentModerationQuery query, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Comment moderation queue loaded.", data = await _service.GetModerationQueueAsync(query, cancellationToken) });
}
