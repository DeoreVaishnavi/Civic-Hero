using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Services;
using CivicHero.Backend.Infrastructure.Configurations;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUser,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _currentUser = currentUser;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = result.Message,
            data = result
        });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
        {
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(
                ["Email and verification token are required."]);
        }

        var session = await _authService.VerifyEmailAsync(request, cancellationToken);
        WriteRefreshCookie(session);
        return Ok(new
        {
            success = true,
            message = "Email verified successfully.",
            data = session.Response
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);
        var session = await _authService.LoginAsync(request, cancellationToken);
        WriteRefreshCookie(session);
        return Ok(new
        {
            success = true,
            message = "Login successful.",
            data = session.Response
        });
    }


    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RequestPasswordResetAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "If the account has a verified phone number, a reset code has been sent.",
            data = result
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Password reset successfully. Sign in using your new password."
        });
    }


    [HttpPost("password-reset/request-link")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordResetLink(
        [FromBody] RequestPasswordResetLinkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RequestPasswordResetLinkAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "If an eligible account exists, a secure password-reset link has been sent.",
            data = result
        });
    }

    [HttpPost("password-reset/validate-link")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidatePasswordResetLink(
        [FromBody] ValidatePasswordResetLinkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ValidatePasswordResetLinkAsync(request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = result.IsValid ? "Password-reset link is valid." : "Password-reset link is invalid or expired.",
            data = result
        });
    }

    [HttpPost("password-reset/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> CompletePasswordResetLink(
        [FromBody] CompletePasswordResetLinkRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.CompletePasswordResetLinkAsync(request, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Password reset successfully. Sign in using your new password."
        });
    }


    [HttpPost("phone/request-login-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPhoneLoginOtp([FromBody] RequestPhoneLoginOtpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Phone number is required."]);
        var result = await _authService.RequestPhoneLoginOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(new { success = true, message = "If the number is eligible, an OTP has been sent.", data = result });
    }

    [HttpPost("phone/login")]
    [AllowAnonymous]
    public async Task<IActionResult> PhoneLogin([FromBody] VerifyPhoneLoginOtpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Code))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Phone number and OTP are required."]);
        var session = await _authService.LoginWithPhoneOtpAsync(request, cancellationToken);
        WriteRefreshCookie(session);
        return Ok(new { success = true, message = "Phone login successful.", data = session.Response });
    }

    [HttpPost("phone/request-verification")]
    [Authorize]
    public async Task<IActionResult> RequestPhoneVerification([FromBody] RequestPhoneVerificationOtpRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        var result = await _authService.RequestPhoneVerificationOtpAsync(userId, request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(new { success = true, message = "Phone verification OTP sent.", data = result });
    }

    [HttpPost("phone/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneNumberRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        var result = await _authService.VerifyPhoneAsync(userId, request, cancellationToken);
        return Ok(new { success = true, message = "Phone number verified successfully. Please refresh your session.", data = result });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[_jwtOptions.RefreshCookieName] ?? request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is missing.");
        }

        var session = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        WriteRefreshCookie(session);
        return Ok(new
        {
            success = true,
            message = "Token refreshed successfully.",
            data = session.Response
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId.HasValue)
        {
            await _authService.LogoutAsync(_currentUser.UserId.Value, User.FindFirst("sid")?.Value, cancellationToken);
        }

        Response.Cookies.Delete(_jwtOptions.RefreshCookieName, GetCookieOptions(DateTimeOffset.UtcNow));
        return Ok(new { success = true, message = "Logout successful." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(new { success = true, message = "Current user loaded.", data = user });
    }

    private void WriteRefreshCookie(AuthSessionResult session) =>
        Response.Cookies.Append(
            _jwtOptions.RefreshCookieName,
            session.RefreshToken,
            GetCookieOptions(session.RefreshTokenExpiresAtUtc));

    private CookieOptions GetCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = expires,
        IsEssential = true,
        Path = "/api/v1/auth"
    };

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T request,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(
                result.Errors.Select(error => error.ErrorMessage));
        }
    }
}
