using CivicHero.Backend.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CivicHero.Backend.Tests;

public class ConfigValidationTests
{
    [Fact]
    public void PlaceholderJwtSecret_IsRejected()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARACTERS"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => JwtConfigurationHelper.Validate(configuration));
        Assert.Contains("Jwt:SecretKey", exception.Message);
    }

    [Fact]
    public void ValidJwtSecret_IsAccepted()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "this-is-a-valid-secret-key-for-local-development-123"
            })
            .Build();

        var exception = Record.Exception(() => JwtConfigurationHelper.Validate(configuration));
        Assert.Null(exception);
    }
}
