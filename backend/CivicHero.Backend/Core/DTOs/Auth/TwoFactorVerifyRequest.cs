
namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed class TwoFactorVerifyRequest
{
    public string Code { get; set; } = string.Empty;
}
