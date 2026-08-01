using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<UpdateProfileRequest> _profileValidator;
    private readonly IValidator<ChangeRoleRequest> _roleValidator;

    public UsersController(
        IUserService userService,
        ICurrentUserService currentUser,
        IValidator<UpdateProfileRequest> profileValidator,
        IValidator<ChangeRoleRequest> roleValidator)
    {
        _userService = userService;
        _currentUser = currentUser;
        _profileValidator = profileValidator;
        _roleValidator = roleValidator;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken) =>
        OkEnvelope("Profile loaded.", await _userService.GetProfileAsync(CurrentUserId(), cancellationToken));

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_profileValidator, request, cancellationToken);
        return OkEnvelope("Profile updated.", await _userService.UpdateProfileAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpGet]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> GetUsers([FromQuery] UserQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Users loaded.", await _userService.GetUsersAsync(query, cancellationToken));

    [HttpGet("metadata")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Metadata(CancellationToken cancellationToken) =>
        OkEnvelope("User-management metadata loaded.", await _userService.GetManagementMetadataAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetUser(long id, CancellationToken cancellationToken) =>
        OkEnvelope("User loaded.", await _userService.GetUserAsync(CurrentUserId(), CurrentRole(), id, cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_profileValidator, request, cancellationToken);
        return OkEnvelope("User updated.", await _userService.UpdateUserAsync(CurrentUserId(), CurrentRole(), id, request, cancellationToken));
    }

    [HttpPost("{id:long}/role")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> ChangeRole(long id, [FromBody] ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_roleValidator, request, cancellationToken);
        return OkEnvelope("Role and scope updated. Existing sessions were revoked.", await _userService.ChangeRoleAsync(CurrentUserId(), CurrentRole(), id, request, cancellationToken));
    }

    [HttpPost("{id:long}/department")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> AssignDepartment(long id, [FromBody] AssignDepartmentRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Department updated. Existing sessions were revoked.", await _userService.AssignDepartmentAsync(CurrentUserId(), CurrentRole(), id, request, cancellationToken));

    [HttpPost("{id:long}/ward")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> AssignWard(long id, [FromBody] AssignWardRequest request, CancellationToken cancellationToken) =>
        OkEnvelope("Ward updated. Existing sessions were revoked.", await _userService.AssignWardAsync(CurrentUserId(), CurrentRole(), id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken) =>
        OkEnvelope("User deactivated and sessions revoked.", await _userService.SetActiveAsync(CurrentUserId(), CurrentRole(), id, false, cancellationToken));

    [HttpPost("{id:long}/activate")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> Activate(long id, CancellationToken cancellationToken) =>
        OkEnvelope("User activated.", await _userService.SetActiveAsync(CurrentUserId(), CurrentRole(), id, true, cancellationToken));

    [HttpPost("{id:long}/force-logout")]
    [Authorize(Policy = PermissionConstants.AdminOrAbove)]
    public async Task<IActionResult> ForceLogout(long id, CancellationToken cancellationToken)
    {
        await _userService.ForceLogoutAsync(CurrentUserId(), CurrentRole(), id, cancellationToken);
        return Ok(new { success = true, message = "All sessions for this user were revoked." });
    }

    private long CurrentUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
    private string CurrentRole() => _currentUser.Role ?? throw new UnauthorizedAccessException("Authenticated role is missing.");
    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(error => error.ErrorMessage));
    }
}
