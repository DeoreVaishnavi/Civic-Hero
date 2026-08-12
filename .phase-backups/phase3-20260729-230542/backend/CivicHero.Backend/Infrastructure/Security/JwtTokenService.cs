using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CivicHero.Backend.Infrastructure.Security;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        JwtConfigurationHelper.Validate(_options.SecretKey);
    }

    public AccessTokenResult GenerateAccessToken(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(Math.Clamp(_options.ExpiryMinutes, 5, 1440));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("auth_version", user.AuthorizationVersion.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (user.DepartmentId.HasValue) claims.Add(new Claim("DepartmentId", user.DepartmentId.Value.ToString()));
        if (user.WardId.HasValue) claims.Add(new Claim("WardId", user.WardId.Value.ToString()));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
