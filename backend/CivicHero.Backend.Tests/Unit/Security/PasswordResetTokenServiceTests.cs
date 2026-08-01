using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

namespace CivicHero.Backend.Tests.Unit.Security;

public sealed class PasswordResetTokenServiceTests : IDisposable
{
    private readonly string _keyDirectory = Path.Combine(Path.GetTempPath(), $"civichero-reset-{Guid.NewGuid():N}");

    [Fact]
    public void CreateToken_ProducesReadableBoundPayload()
    {
        Directory.CreateDirectory(_keyDirectory);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(_keyDirectory));
        var service = new PasswordResetTokenService(provider);
        var user = new User
        {
            Id = 42,
            Email = "Citizen@Example.com",
            AuthorizationVersion = 7,
            Role = UserRole.Citizen
        };

        var token = service.CreateToken(user, TimeSpan.FromMinutes(30));
        var valid = service.TryReadToken(token, out var payload, out var expiresAtUtc);

        Assert.True(valid);
        Assert.NotNull(payload);
        Assert.Equal(user.Id, payload!.UserId);
        Assert.Equal("citizen@example.com", payload.Email);
        Assert.Equal(user.AuthorizationVersion, payload.AuthorizationVersion);
        Assert.True(expiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TryReadToken_RejectsTamperedToken()
    {
        Directory.CreateDirectory(_keyDirectory);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(_keyDirectory));
        var service = new PasswordResetTokenService(provider);
        var user = new User { Id = 9, Email = "user@example.com", AuthorizationVersion = 1 };
        var token = service.CreateToken(user, TimeSpan.FromMinutes(30));
        var finalCharacter = token[^1] == 'A' ? 'B' : 'A';
        var tampered = token[..^1] + finalCharacter;

        var valid = service.TryReadToken(tampered, out var payload, out _);

        Assert.False(valid);
        Assert.Null(payload);
    }

    public void Dispose()
    {
        if (Directory.Exists(_keyDirectory)) Directory.Delete(_keyDirectory, true);
    }
}
