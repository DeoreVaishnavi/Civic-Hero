namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed class LoginRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }

    public string EffectiveIdentifier => string.IsNullOrWhiteSpace(Identifier) ? Email ?? string.Empty : Identifier;
}
