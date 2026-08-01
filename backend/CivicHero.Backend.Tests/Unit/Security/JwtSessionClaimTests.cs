using System.IdentityModel.Tokens.Jwt;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Tests.Unit.Security;

public sealed class JwtSessionClaimTests
{
    [Fact]
    public void Access_token_should_include_the_account_session_identifier()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "CivicHero.Tests",
            Audience = "CivicHero.TestUsers",
            SecretKey = "CivicHero_Change31F_Test_Key_Only_64_Characters_Minimum_1234567890",
            ExpiryMinutes = 60
        }));
        var user = new User
        {
            Id = 42,
            Email = "citizen@example.com",
            FullName = "Citizen Test",
            Role = UserRole.Citizen,
            AuthorizationVersion = 7
        };

        var result = service.GenerateAccessToken(user, "session-abc-123");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        token.Claims.Single(x => x.Type == "sid").Value.Should().Be("session-abc-123");
        token.Claims.Single(x => x.Type == "auth_version").Value.Should().Be("7");
    }
}
