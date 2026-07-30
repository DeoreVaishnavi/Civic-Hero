
using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/security/two-factor")]
[Authorize]
public sealed class TwoFactorController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ITwoFactorService _twoFactor;
    public TwoFactorController(IUserRepository users, IUnitOfWork unitOfWork, ICurrentUserService currentUser, ITwoFactorService twoFactor)
    { _users = users; _unitOfWork = unitOfWork; _currentUser = currentUser; _twoFactor = twoFactor; }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    { var user = await LoadAsync(cancellationToken); return OkEnvelope("Two-factor status loaded.", _twoFactor.GetStatus(user)); }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken cancellationToken)
    { var user = await LoadAsync(cancellationToken); var result = _twoFactor.BeginSetup(user); _users.Update(user); await _unitOfWork.SaveChangesAsync(cancellationToken); return OkEnvelope("Scan the QR-compatible URI or enter the key manually, then confirm a six-digit code.", result); }

    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    { Validate(request); var user = await LoadAsync(cancellationToken); var result = _twoFactor.Enable(user, request.Code); _users.Update(user); await _unitOfWork.SaveChangesAsync(cancellationToken); return OkEnvelope("Two-factor authentication enabled. Store the recovery codes securely and sign in again.", result); }

    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    { Validate(request); var user = await LoadAsync(cancellationToken); _twoFactor.Disable(user, request.Code); _users.Update(user); await _unitOfWork.SaveChangesAsync(cancellationToken); return OkEnvelope("Two-factor authentication disabled. Sign in again.", new { enabled = false, requiresSignInAgain = true }); }

    [HttpPost("recovery-codes")]
    public async Task<IActionResult> RecoveryCodes([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    { Validate(request); var user = await LoadAsync(cancellationToken); var codes = _twoFactor.RegenerateRecoveryCodes(user, request.Code); _users.Update(user); await _unitOfWork.SaveChangesAsync(cancellationToken); return OkEnvelope("New recovery codes generated. Previous codes are invalid.", new { recoveryCodes = codes }); }

    private async Task<CivicHero.Backend.Core.Entities.User> LoadAsync(CancellationToken cancellationToken) => await _users.GetByIdAsync(_currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing."), cancellationToken) ?? throw new NotFoundException("User account was not found.");
    private static void Validate(TwoFactorVerifyRequest request) { if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 40) throw new CivicHero.Backend.Core.Exceptions.ValidationException(["A valid authenticator or recovery code is required."]); }
    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}
