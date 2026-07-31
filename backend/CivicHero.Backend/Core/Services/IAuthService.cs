using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.DTOs.Users;

namespace CivicHero.Backend.Core.Services;

public sealed record AuthSessionResult(AuthResponse Response, string RefreshToken, DateTimeOffset RefreshTokenExpiresAtUtc);

public interface IAuthService
{
    Task<RegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionResult> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<PhoneOtpRequestResponse> RequestPasswordResetAsync(ForgotPasswordRequest request, string? remoteIp, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<PhoneOtpRequestResponse> RequestPhoneLoginOtpAsync(RequestPhoneLoginOtpRequest request, string? remoteIp, CancellationToken cancellationToken = default);
    Task<AuthSessionResult> LoginWithPhoneOtpAsync(VerifyPhoneLoginOtpRequest request, CancellationToken cancellationToken = default);
    Task<PhoneOtpRequestResponse> RequestPhoneVerificationOtpAsync(long userId, RequestPhoneVerificationOtpRequest request, string? remoteIp, CancellationToken cancellationToken = default);
    Task<PhoneVerificationStatusResponse> VerifyPhoneAsync(long userId, VerifyPhoneNumberRequest request, CancellationToken cancellationToken = default);
    Task<AuthSessionResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
}
