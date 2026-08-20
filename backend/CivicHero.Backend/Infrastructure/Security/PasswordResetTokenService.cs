using System.Security.Cryptography;
using System.Text.Json;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace CivicHero.Backend.Infrastructure.Security;

public sealed class PasswordResetTokenService : IPasswordResetTokenService
{
    private const string ProtectorPurpose = "CivicHero.PasswordResetLink.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITimeLimitedDataProtector _protector;

    public PasswordResetTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();
    }

    public string CreateToken(User user, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Token lifetime must be positive.");

        var payload = new PasswordResetTokenData(
            user.Id,
            user.Email.Trim().ToLowerInvariant(),
            user.AuthorizationVersion,
            Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));

        return _protector.Protect(JsonSerializer.Serialize(payload, JsonOptions), lifetime);
    }

    public bool TryReadToken(
        string token,
        out PasswordResetTokenData? data,
        out DateTimeOffset expiresAtUtc)
    {
        data = null;
        expiresAtUtc = default;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var json = _protector.Unprotect(token.Trim(), out expiresAtUtc);
            data = JsonSerializer.Deserialize<PasswordResetTokenData>(json, JsonOptions);
            return data is not null &&
                   data.UserId > 0 &&
                   !string.IsNullOrWhiteSpace(data.Email) &&
                   data.AuthorizationVersion > 0 &&
                   !string.IsNullOrWhiteSpace(data.Nonce) &&
                   expiresAtUtc > DateTimeOffset.UtcNow;
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or ArgumentException)
        {
            data = null;
            expiresAtUtc = default;
            return false;
        }
    }
}
