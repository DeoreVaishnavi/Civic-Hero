using CivicHero.Backend.Core.DTOs.Users;

namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public UserDto User { get; set; } = new();
    public bool RequiresTwoFactorSetup { get; set; }
    public DateTimeOffset? TwoFactorSetupDeadlineUtc { get; set; }
}

public sealed class RegistrationResponse
{
    public string Email { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; } = true;
    public string Message { get; set; } = "Registration successful. Verify your email before logging in.";
    public string? DevelopmentVerificationToken { get; set; }
}
