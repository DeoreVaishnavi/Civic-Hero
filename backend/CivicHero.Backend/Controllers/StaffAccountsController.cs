using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.DTOs.Staff;
using CivicHero.Backend.Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/staff-accounts")]
[Authorize(Policy = PermissionConstants.AdminOrAbove)]
public sealed class StaffAccountsController : ControllerBase
{
    private readonly IStaffAccountService _service;
    private readonly IValidator<CreateStaffAccountRequest> _createValidator;
    private readonly IValidator<ReviewStaffAccountRequest> _reviewValidator;

    public StaffAccountsController(
        IStaffAccountService service,
        IValidator<CreateStaffAccountRequest> createValidator,
        IValidator<ReviewStaffAccountRequest> reviewValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _reviewValidator = reviewValidator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] StaffAccountQuery query, CancellationToken cancellationToken) =>
        OkEnvelope("Staff accounts loaded.", await _service.GetAsync(query, cancellationToken));

    [HttpGet("pending")]
    [Authorize(Policy = PermissionConstants.SuperAdminOnly)]
    public async Task<IActionResult> Pending([FromQuery] string? search, CancellationToken cancellationToken) =>
        OkEnvelope("Pending staff approvals loaded.", await _service.GetAsync(
            new StaffAccountQuery { Search = search, Status = "PendingApproval" }, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffAccountRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        var created = await _service.CreateAsync(request, cancellationToken);
        var message = created.ApprovalStatus == "PendingApproval"
            ? "Staff account created and sent to SuperAdmin for approval."
            : "Staff account created and approved.";
        return OkEnvelope(message, created);
    }

    [HttpPost("{userId:long}/review")]
    [Authorize(Policy = PermissionConstants.SuperAdminOnly)]
    public async Task<IActionResult> Review(long userId, [FromBody] ReviewStaffAccountRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_reviewValidator, request, cancellationToken);
        var reviewed = await _service.ReviewAsync(userId, request, cancellationToken);
        return OkEnvelope($"Staff account {reviewed.ApprovalStatus.ToLowerInvariant()}.", reviewed);
    }

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(result.Errors.Select(error => error.ErrorMessage));
    }
}
