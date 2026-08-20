using System.Text;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Configuration;

namespace CivicHero.Backend.Infrastructure.Security;

public static class JwtConfigurationHelper
{
    private static readonly string[] PlaceholderPrefixes = ["CHANGE_THIS", "YOUR_", "INSERT_"]; 

    public static string ResolveSecret(IConfiguration configuration)
    {
        var secret = configuration[$"{JwtOptions.SectionName}:SecretKey"]?.Trim();
        Validate(secret);
        return secret!;
    }

    public static void Validate(IConfiguration configuration)
    {
        var secret = configuration[$"{JwtOptions.SectionName}:SecretKey"]?.Trim();
        Validate(secret);
    }

    public static void Validate(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Jwt:SecretKey is missing. Run configure-phase3-secrets.ps1 or set Jwt__SecretKey.");

        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:SecretKey must contain at least 32 bytes. Run configure-phase3-secrets.ps1.");

        if (PlaceholderPrefixes.Any(prefix => secret.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Jwt:SecretKey contains a placeholder value. Run configure-phase3-secrets.ps1.");
    }
}
