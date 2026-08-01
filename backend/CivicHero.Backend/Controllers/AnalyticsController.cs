using CivicHero.Backend.Core.DTOs.Analytics;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
    private const string ManagementAnalyticsRoles = "Supervisor,Admin,SuperAdmin";
    private const string HeatmapRoles = "Citizen,Officer,Supervisor,Admin,SuperAdmin";
    private readonly IAnalyticsService _service;

    public AnalyticsController(IAnalyticsService service) => _service = service;

    [HttpGet("overview")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Overview([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Analytics overview loaded.", await _service.GetOverviewAsync(filter, cancellationToken));

    [HttpGet("complaints")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Complaints([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Complaint analytics loaded.", await _service.GetComplaintAnalyticsAsync(filter, cancellationToken));

    [HttpGet("departments")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Departments([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Department analytics loaded.", await _service.GetDepartmentAnalyticsAsync(filter, cancellationToken));

    [HttpGet("officers")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Officers([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Officer analytics loaded.", await _service.GetOfficerAnalyticsAsync(filter, cancellationToken));

    [HttpGet("wards")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Wards([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Ward analytics loaded.", await _service.GetWardAnalyticsAsync(filter, cancellationToken));

    [HttpGet("sla")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Sla([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("SLA analytics loaded.", await _service.GetSlaAnalyticsAsync(filter, cancellationToken));

    [HttpGet("satisfaction")]
    [Authorize(Roles = ManagementAnalyticsRoles)]
    public async Task<IActionResult> Satisfaction([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Satisfaction analytics loaded.", await _service.GetSatisfactionAnalyticsAsync(filter, cancellationToken));

    [HttpGet("heatmap")]
    [Authorize(Roles = HeatmapRoles)]
    public async Task<IActionResult> Heatmap([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Geographic heatmap data loaded.", await _service.GetHeatmapAsync(filter, cancellationToken));

    [HttpGet("public/heatmap")]
    [AllowAnonymous]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> PublicHeatmap([FromQuery] AnalyticsFilter filter, CancellationToken cancellationToken) =>
        OkEnvelope("Sanitized public geographic heatmap data loaded.", await _service.GetPublicHeatmapAsync(filter, cancellationToken));

    [HttpGet("export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Export([FromQuery] string report, [FromQuery] string format = "csv", [FromQuery] AnalyticsFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var file = await _service.ExportAsync(report, format, filter ?? new AnalyticsFilter(), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
