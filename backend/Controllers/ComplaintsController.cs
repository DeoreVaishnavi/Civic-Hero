using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/complaints")]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _service;

    public ComplaintsController(
        IComplaintService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateComplaint(
        ComplaintCreateRequest request)
    {
        var result =
            await _service.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetComplaint),
            new { id = result.Id },
            result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetComplaint(int id)
    {
        var result =
            await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetComplaints(
        [FromQuery] ComplaintQueryParameters query)
    {
        var result =
            await _service.GetAllAsync(query);

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateComplaint(
        int id,
        ComplaintUpdateRequest request)
    {
        bool updated =
            await _service.UpdateAsync(id, request);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id:int}/assign-officer")]
    public async Task<IActionResult> AssignOfficer(
        int id,
        AssignOfficerRequest request)
    {
        bool result =
            await _service.AssignOfficerAsync(
                id,
                request);

        if (!result)
            return NotFound();

        return Ok(new
        {
            message = "Officer assigned successfully."
        });
    }

    [HttpPut("{id:int}/assign-contractor")]
    public async Task<IActionResult> AssignContractor(
        int id,
        AssignContractorRequest request)
    {
        bool result =
            await _service.AssignContractorAsync(
                id,
                request);

        if (!result)
            return NotFound();

        return Ok(new
        {
            message = "Contractor assigned successfully."
        });
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        StatusUpdateRequest request)
    {
        try
        {
            bool result =
                await _service.UpdateStatusAsync(
                    id,
                    request);

            if (!result)
                return NotFound();

            return Ok(new
            {
                message =
                    "Complaint status updated successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("{id:int}/timeline-notes")]
    public async Task<IActionResult> AddTimelineNote(
        int id,
        TimelineNoteCreateDto request)
    {
        bool result =
            await _service.AddTimelineNoteAsync(
                id,
                request);

        if (!result)
            return NotFound();

        return Ok(new
        {
            message = "Timeline note added."
        });
    }

    [HttpGet("{id:int}/timeline")]
    public async Task<IActionResult> GetTimeline(int id)
    {
        var result =
            await _service.GetTimelineAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        return Ok(
            await _service.GetDashboardAsync());
    }

    [HttpGet("stats/summary")]
    public async Task<IActionResult> GetStats()
    {
        return Ok(
            await _service.GetDashboardAsync());
    }

    [HttpGet("ward/{wardId:int}")]
    public async Task<IActionResult> GetWardComplaints(
        int wardId)
    {
        return Ok(
            await _service.GetByWardAsync(wardId));
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyComplaints(
        double latitude,
        double longitude,
        double radius = 5)
    {
        if (latitude is < -90 or > 90)
            return BadRequest("Invalid latitude.");

        if (longitude is < -180 or > 180)
            return BadRequest("Invalid longitude.");

        if (radius <= 0)
            return BadRequest("Radius must be positive.");

        return Ok(
            await _service.GetNearbyAsync(
                latitude,
                longitude,
                radius));
    }

    [HttpPut("{id:int}/archive")]
    public async Task<IActionResult> ArchiveComplaint(
        int id)
    {
        bool result =
            await _service.ArchiveAsync(id);

        if (!result)
            return NotFound();

        return Ok(new
        {
            message = "Complaint archived successfully."
        });
    }
}