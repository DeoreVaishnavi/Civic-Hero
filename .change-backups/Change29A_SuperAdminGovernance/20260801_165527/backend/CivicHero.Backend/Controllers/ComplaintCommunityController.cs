using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/complaints")]
[Authorize(Policy = PermissionConstants.CitizenOnly)]
public sealed class ComplaintCommunityController : ControllerBase
{
    private readonly IComplaintCommunityService _community;

    public ComplaintCommunityController(IComplaintCommunityService community) => _community = community;

    [HttpPost("{complaintId:long}/follow")]
    public async Task<IActionResult> Follow(long complaintId, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Complaint added to your following feed.", data = await _community.FollowAsync(complaintId, cancellationToken) });

    [HttpDelete("{complaintId:long}/follow")]
    public async Task<IActionResult> Unfollow(long complaintId, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Complaint removed from your following feed.", data = await _community.UnfollowAsync(complaintId, cancellationToken) });

    [HttpGet("{complaintId:long}/follow-status")]
    public async Task<IActionResult> FollowStatus(long complaintId, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Complaint follow status loaded.", data = await _community.GetFollowStatusAsync(complaintId, cancellationToken) });

    [HttpGet("following/ids")]
    public async Task<IActionResult> FollowingIds(CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Followed complaint identifiers loaded.", data = await _community.GetFollowingIdsAsync(cancellationToken) });

    [HttpGet("following")]
    public async Task<IActionResult> Following([FromQuery] ComplaintQuery query, CancellationToken cancellationToken) =>
        Ok(new { success = true, message = "Following feed loaded.", data = await _community.GetFollowingAsync(query, cancellationToken) });
}
