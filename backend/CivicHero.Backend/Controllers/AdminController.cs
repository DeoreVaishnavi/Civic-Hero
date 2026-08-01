using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class AdminController : ControllerBase
{
    private readonly IAdministrationService _service;
    public AdminController(IAdministrationService service) => _service = service;

    [HttpGet("overview")] public async Task<IActionResult> Overview(CancellationToken ct) => OkEnvelope("Administration overview loaded.", await _service.GetOverviewAsync(ct));

    [HttpGet("categories")] public async Task<IActionResult> Categories(CancellationToken ct) => OkEnvelope("Categories loaded.", await _service.GetCategoriesAsync(ct));
    [HttpPost("categories")] public async Task<IActionResult> CreateCategory([FromBody] SaveCategoryRequest request, CancellationToken ct) => OkEnvelope("Category created.", await _service.CreateCategoryAsync(request, ct));
    [HttpPut("categories/{id:long}")] public async Task<IActionResult> UpdateCategory(long id, [FromBody] SaveCategoryRequest request, CancellationToken ct) => OkEnvelope("Category updated.", await _service.UpdateCategoryAsync(id, request, ct));
    [HttpPost("categories/{id:long}/activate")] public async Task<IActionResult> ActivateCategory(long id, CancellationToken ct) { await _service.SetCategoryActiveAsync(id, true, ct); return OkEnvelope("Category activated.", new { id }); }
    [HttpDelete("categories/{id:long}")] public async Task<IActionResult> DeactivateCategory(long id, CancellationToken ct) { await _service.SetCategoryActiveAsync(id, false, ct); return OkEnvelope("Category deactivated.", new { id }); }

    [HttpGet("departments")] public async Task<IActionResult> Departments(CancellationToken ct) => OkEnvelope("Departments loaded.", await _service.GetDepartmentsAsync(ct));
    [HttpPost("departments")] public async Task<IActionResult> CreateDepartment([FromBody] SaveDepartmentRequest request, CancellationToken ct) => OkEnvelope("Department created.", await _service.CreateDepartmentAsync(request, ct));
    [HttpPut("departments/{id:long}")] public async Task<IActionResult> UpdateDepartment(long id, [FromBody] SaveDepartmentRequest request, CancellationToken ct) => OkEnvelope("Department updated.", await _service.UpdateDepartmentAsync(id, request, ct));
    [HttpPost("departments/{id:long}/activate")] public async Task<IActionResult> ActivateDepartment(long id, CancellationToken ct) { await _service.SetDepartmentActiveAsync(id, true, ct); return OkEnvelope("Department activated.", new { id }); }
    [HttpDelete("departments/{id:long}")] public async Task<IActionResult> DeactivateDepartment(long id, CancellationToken ct) { await _service.SetDepartmentActiveAsync(id, false, ct); return OkEnvelope("Department and its wards deactivated.", new { id }); }

    [HttpGet("wards")] public async Task<IActionResult> Wards([FromQuery] long? departmentId, CancellationToken ct) => OkEnvelope("Wards loaded.", await _service.GetWardsAsync(departmentId, ct));
    [HttpPost("wards")] public async Task<IActionResult> CreateWard([FromBody] SaveWardRequest request, CancellationToken ct) => OkEnvelope("Ward created.", await _service.CreateWardAsync(request, ct));
    [HttpPut("wards/{id:long}")] public async Task<IActionResult> UpdateWard(long id, [FromBody] SaveWardRequest request, CancellationToken ct) => OkEnvelope("Ward updated.", await _service.UpdateWardAsync(id, request, ct));
    [HttpPost("wards/{id:long}/activate")] public async Task<IActionResult> ActivateWard(long id, CancellationToken ct) { await _service.SetWardActiveAsync(id, true, ct); return OkEnvelope("Ward activated.", new { id }); }
    [HttpDelete("wards/{id:long}")] public async Task<IActionResult> DeactivateWard(long id, CancellationToken ct) { await _service.SetWardActiveAsync(id, false, ct); return OkEnvelope("Ward deactivated.", new { id }); }

    [HttpGet("settings")] public async Task<IActionResult> Settings(CancellationToken ct) => OkEnvelope("System settings loaded.", await _service.GetSettingsAsync(ct));
    [HttpPut("settings/{key}")][Authorize(Policy = PermissionConstants.SuperAdminOnly)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSystemSettingRequest request, CancellationToken ct) => OkEnvelope("System setting updated.", await _service.UpdateSettingAsync(key, request, ct));

    [HttpGet("audit-logs")] public async Task<IActionResult> AuditLogs([FromQuery] AuditLogQuery query, CancellationToken ct) => OkEnvelope("Audit logs loaded.", await _service.GetAuditLogsAsync(query, ct));
    [HttpGet("audit-logs/export")]
    public async Task<IActionResult> ExportAuditLogs([FromQuery] AuditLogQuery query, [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        var file = await _service.ExportAuditLogsAsync(query, format, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("system-health")] public async Task<IActionResult> SystemHealth(CancellationToken ct) => OkEnvelope("System health loaded.", await _service.GetSystemHealthAsync(ct));
    [HttpPost("maintenance/cleanup")][Authorize(Policy = PermissionConstants.SuperAdminOnly)]
    public async Task<IActionResult> Cleanup([FromQuery] bool dryRun = true, [FromQuery] int retentionDays = 365, CancellationToken ct = default) => OkEnvelope(dryRun ? "Maintenance preview completed." : "Maintenance cleanup completed.", await _service.CleanupAsync(dryRun, retentionDays, ct));

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
