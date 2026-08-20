namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed class RequestPasswordResetLinkRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class PasswordResetLinkRequestResponse
{
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string? DevelopmentResetUrl { get; set; }
}

public sealed class ValidatePasswordResetLinkRequest
{
    public string Token { get; set; } = string.Empty;
}

public sealed class PasswordResetLinkStatusResponse
{
    public bool IsValid { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

public sealed class CompletePasswordResetLinkRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
