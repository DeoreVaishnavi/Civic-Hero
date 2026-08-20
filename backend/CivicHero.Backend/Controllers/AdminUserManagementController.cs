using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/admin/user-management")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class AdminUserManagementController : ControllerBase
{
    private readonly IAdminUserManagementService _service;
    public AdminUserManagementController(IAdminUserManagementService service) => _service = service;

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] AdminUserManagementQuery query, CancellationToken ct) =>
        OkEnvelope("Managed users loaded.", await _service.GetUsersAsync(query, ct));

    [HttpPost("citizens")]
    public async Task<IActionResult> CreateCitizen([FromBody] AdminCreateCitizenRequest request, CancellationToken ct) =>
        OkEnvelope("Citizen account created.", await _service.CreateCitizenAsync(request, ct));

    [HttpPut("users/{userId:long}/email")]
    public async Task<IActionResult> UpdateEmail(long userId, [FromBody] AdminUpdateUserEmailRequest request, CancellationToken ct) =>
        OkEnvelope("User email updated and existing sessions revoked.", await _service.UpdateEmailAsync(userId, request, ct));

    [HttpDelete("users/{userId:long}")]
    public async Task<IActionResult> SoftDelete(long userId, [FromBody] AdminUserLifecycleRequest request, CancellationToken ct) =>
        OkEnvelope("User account soft-deleted and sessions revoked.", await _service.SoftDeleteAsync(userId, request, ct));

    [HttpPost("users/{userId:long}/restore")]
    public async Task<IActionResult> Restore(long userId, [FromBody] AdminUserLifecycleRequest request, CancellationToken ct) =>
        OkEnvelope("User account restored.", await _service.RestoreAsync(userId, request, ct));

    [HttpGet("users/{userId:long}/history")]
    public async Task<IActionResult> History(long userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        OkEnvelope("User account history loaded.", await _service.GetHistoryAsync(userId, page, pageSize, false, ct));

    [HttpGet("users/{userId:long}/role-history")]
    public async Task<IActionResult> RoleHistory(long userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        OkEnvelope("User role history loaded.", await _service.GetHistoryAsync(userId, page, pageSize, true, ct));

    [HttpGet("department-heads")]
    public async Task<IActionResult> DepartmentHeads(CancellationToken ct) =>
        OkEnvelope("Department-head assignments loaded.", await _service.GetDepartmentHeadsAsync(ct));

    [HttpPut("department-heads/{departmentId:long}")]
    public async Task<IActionResult> AssignDepartmentHead(long departmentId, [FromBody] DepartmentHeadAssignmentRequest request, CancellationToken ct) =>
        OkEnvelope("Department head assigned.", await _service.AssignDepartmentHeadAsync(departmentId, request, ct));

    [HttpDelete("department-heads/{departmentId:long}")]
    public async Task<IActionResult> RemoveDepartmentHead(long departmentId, [FromBody] DepartmentHeadRemovalRequest request, CancellationToken ct)
    {
        await _service.RemoveDepartmentHeadAsync(departmentId, request, ct);
        return OkEnvelope("Department-head assignment removed.", new { departmentId });
    }

    [HttpGet("master-data")]
    public async Task<IActionResult> MasterData(CancellationToken ct) =>
        OkEnvelope("Master-data policy loaded.", await _service.GetMasterDataAsync(ct));

    [HttpPut("master-data")]
    [Authorize(Policy = PermissionConstants.SuperAdminOnly)]
    public async Task<IActionResult> UpdateMasterData([FromBody] AdminMasterDataConfigurationDto request, CancellationToken ct) =>
        OkEnvelope("Master-data policy updated.", await _service.UpdateMasterDataAsync(request, ct));

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
