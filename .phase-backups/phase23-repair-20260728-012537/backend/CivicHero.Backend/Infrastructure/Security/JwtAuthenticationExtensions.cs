using System.Security.Claims;
using System.Text;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CivicHero.Backend.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddCivicHeroJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (Encoding.UTF8.GetByteCount(options.SecretKey) < 32 ||
            options.SecretKey.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must contain at least 32 bytes. Run configure-phase3-secrets.ps1.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.RequireHttpsMetadata = !string.Equals(
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Development",
                    StringComparison.OrdinalIgnoreCase);
                jwt.SaveToken = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        return services;
    }
}
