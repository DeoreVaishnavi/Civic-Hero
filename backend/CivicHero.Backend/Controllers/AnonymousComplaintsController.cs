using CivicHero.Backend.Core.DTOs.Anonymous;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/anonymous-complaints")]
[AllowAnonymous]
public sealed class AnonymousComplaintsController : ControllerBase
{
    private readonly IAnonymousComplaintService _service;
    public AnonymousComplaintsController(IAnonymousComplaintService service) => _service = service;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateAnonymousComplaintRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { success = true, message = "Anonymous complaint submitted securely.", data = result });
    }

    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] TrackAnonymousComplaintRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceNumber) || string.IsNullOrWhiteSpace(request.TrackingToken))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Complaint reference and tracking token are required."]);
        var result = await _service.TrackAsync(request.ReferenceNumber, request.TrackingToken, cancellationToken);
        return Ok(new { success = true, message = "Anonymous complaint status loaded.", data = result });
    }
}
