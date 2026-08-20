namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    public string Identifier { get; set; } = string.Empty;
}
