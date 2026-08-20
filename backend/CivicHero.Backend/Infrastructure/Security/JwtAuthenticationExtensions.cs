using System.Security.Claims;
using System.Text;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CivicHero.Backend.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddCivicHeroJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (Encoding.UTF8.GetByteCount(options.SecretKey) < 32 || options.SecretKey.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Jwt:SecretKey must contain at least 32 bytes. Run configure-phase3-secrets.ps1.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwt =>
        {
            jwt.RequireHttpsMetadata = !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
            jwt.SaveToken = false;
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = options.Issuer,
                ValidateAudience = true, ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
            };
            jwt.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (!long.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                        !int.TryParse(principal?.FindFirstValue("auth_version"), out var authVersion))
                    {
                        context.Fail("Required authorization claims are missing.");
                        return;
                    }

                    var db = context.HttpContext.RequestServices.GetRequiredService<CivicDbContext>();
                    var sessionId = principal?.FindFirstValue("sid");
                    if (!string.IsNullOrWhiteSpace(sessionId))
                    {
                        var now = DateTimeOffset.UtcNow;
                        var session = await db.UserSessions.AsNoTracking()
                            .Where(x => x.SessionId == sessionId && x.UserId == userId)
                            .Select(x => new
                            {
                                x.AuthorizationVersion,
                                x.ExpiresAt,
                                x.RevokedAt,
                                UserActive = x.User.IsActive,
                                UserVerified = x.User.IsEmailVerified,
                                UserAuthorizationVersion = x.User.AuthorizationVersion,
                                UserRole = x.User.Role
                            })
                            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= now ||
                            session.AuthorizationVersion != authVersion || session.UserAuthorizationVersion != authVersion ||
                            !session.UserActive || !session.UserVerified ||
                            !string.Equals(session.UserRole.ToString(), principal?.FindFirstValue(ClaimTypes.Role), StringComparison.Ordinal))
                        {
                            context.Fail("This session is no longer valid.");
                        }

                        return;
                    }

                    // Transitional support for access tokens issued before Change31F. They remain
                    // subject to the account authorization version and expire normally.
                    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, context.HttpContext.RequestAborted);
                    if (user is null || !user.IsActive || !user.IsEmailVerified || user.AuthorizationVersion != authVersion ||
                        !string.Equals(user.Role.ToString(), principal?.FindFirstValue(ClaimTypes.Role), StringComparison.Ordinal))
                    {
                        context.Fail("This session is no longer valid.");
                    }
                }
            };
        });
        return services;
    }
}
