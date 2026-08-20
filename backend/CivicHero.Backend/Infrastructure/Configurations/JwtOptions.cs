namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CivicHero";
    public string Audience { get; set; } = "CivicHeroUsers";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public string RefreshCookieName { get; set; } = "civichero_refresh";
}
