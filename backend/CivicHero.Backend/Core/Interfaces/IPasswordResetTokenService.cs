using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

public sealed record PasswordResetTokenData(
    long UserId,
    string Email,
    int AuthorizationVersion,
    string Nonce);

public interface IPasswordResetTokenService
{
    string CreateToken(User user, TimeSpan lifetime);

    bool TryReadToken(
        string token,
        out PasswordResetTokenData? data,
        out DateTimeOffset expiresAtUtc);
}
