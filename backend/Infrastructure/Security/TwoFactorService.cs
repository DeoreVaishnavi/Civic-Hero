
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Exceptions;
using Microsoft.AspNetCore.DataProtection;

namespace CivicHero.Backend.Infrastructure.Security;

public sealed class TwoFactorService : ITwoFactorService
{
    private const int StepSeconds = 30;
    private readonly IDataProtector _protector;
    private readonly IConfiguration _configuration;
    public TwoFactorService(IDataProtectionProvider provider, IConfiguration configuration)
    { _protector = provider.CreateProtector("CivicHero.TwoFactor.v1"); _configuration = configuration; }

    public TwoFactorStatusResponse GetStatus(User user) => new(user.TwoFactorEnabled,
        !user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TwoFactorSecretProtected), user.TwoFactorEnabledAt, ReadRecoveryHashes(user).Count);

    public TwoFactorSetupResponse BeginSetup(User user)
    {
        if (user.TwoFactorEnabled) throw new BusinessRuleViolationException("Two-factor authentication is already enabled.");
        var secret = Base32Encode(RandomNumberGenerator.GetBytes(20));
        user.TwoFactorSecretProtected = _protector.Protect(secret);
        user.TwoFactorRecoveryCodesJson = null;
        var issuer = _configuration["Security:TwoFactorIssuer"] ?? "CivicHero";
        var label = $"{issuer}:{user.Email}";
        var uri = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period={StepSeconds}";
        return new TwoFactorSetupResponse(secret, uri, issuer, user.Email);
    }

    public TwoFactorEnableResponse Enable(User user, string code)
    {
        var secret = ReadSecret(user);
        if (!VerifyTotp(secret, code)) throw new BusinessRuleViolationException("The authenticator code is invalid.");
        var recoveryCodes = GenerateRecoveryCodes();
        user.TwoFactorEnabled = true;
        user.TwoFactorEnabledAt = DateTimeOffset.UtcNow;
        user.TwoFactorRecoveryCodesJson = JsonSerializer.Serialize(recoveryCodes.Select(HashCode));
        RevokeSessions(user);
        return new TwoFactorEnableResponse(true, recoveryCodes, true);
    }

    public void Disable(User user, string code)
    {
        EnsureEnabled(user);
        if (!VerifyTotp(ReadSecret(user), code) && !ConsumeRecoveryCode(user, code))
            throw new BusinessRuleViolationException("The authenticator or recovery code is invalid.");
        user.TwoFactorEnabled = false;
        user.TwoFactorSecretProtected = null;
        user.TwoFactorRecoveryCodesJson = null;
        user.TwoFactorEnabledAt = null;
        RevokeSessions(user);
    }

    public IReadOnlyList<string> RegenerateRecoveryCodes(User user, string code)
    {
        EnsureEnabled(user);
        if (!VerifyTotp(ReadSecret(user), code) && !ConsumeRecoveryCode(user, code))
            throw new BusinessRuleViolationException("The authenticator or recovery code is invalid.");
        var codes = GenerateRecoveryCodes();
        user.TwoFactorRecoveryCodesJson = JsonSerializer.Serialize(codes.Select(HashCode));
        return codes;
    }

    public void VerifyForLogin(User user, string? code)
    {
        if (!user.TwoFactorEnabled) return;
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessRuleViolationException("Authenticator or recovery code is required for this account.");
        if (VerifyTotp(ReadSecret(user), code) || ConsumeRecoveryCode(user, code)) return;
        throw new UnauthorizedAccessException("The two-factor authentication code is invalid.");
    }

    private string ReadSecret(User user)
    {
        if (string.IsNullOrWhiteSpace(user.TwoFactorSecretProtected)) throw new BusinessRuleViolationException("Two-factor setup has not been started.");
        try { return _protector.Unprotect(user.TwoFactorSecretProtected); }
        catch (CryptographicException) { throw new BusinessRuleViolationException("The two-factor secret cannot be decrypted. Contact an administrator."); }
    }

    private bool ConsumeRecoveryCode(User user, string code)
    {
        var hashes = ReadRecoveryHashes(user);
        var target = HashCode(code);
        var index = hashes.FindIndex(value => FixedEquals(value, target));
        if (index < 0) return false;
        hashes.RemoveAt(index);
        user.TwoFactorRecoveryCodesJson = JsonSerializer.Serialize(hashes);
        return true;
    }

    private static List<string> ReadRecoveryHashes(User user)
    {
        if (string.IsNullOrWhiteSpace(user.TwoFactorRecoveryCodesJson)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(user.TwoFactorRecoveryCodesJson) ?? []; }
        catch (JsonException) { return []; }
    }

    private static List<string> GenerateRecoveryCodes() => Enumerable.Range(0, 8)
        .Select(_ => $"{Base32Encode(RandomNumberGenerator.GetBytes(8))[..8]}-{Base32Encode(RandomNumberGenerator.GetBytes(8))[..8]}")
        .ToList();

    private static bool VerifyTotp(string secret, string? code)
    {
        var normalized = (code ?? string.Empty).Replace(" ", string.Empty).Trim();
        if (normalized.Length != 6 || !normalized.All(char.IsDigit)) return false;
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;
        return Enumerable.Range(-1, 3).Any(offset => FixedEquals(GenerateTotp(secret, counter + offset), normalized));
    }

    private static string GenerateTotp(string secret, long counter)
    {
        var key = Base32Decode(secret);
        var bytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(bytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static string HashCode(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant())));
    private static bool FixedEquals(string first, string second) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));
    private static void EnsureEnabled(User user) { if (!user.TwoFactorEnabled) throw new BusinessRuleViolationException("Two-factor authentication is not enabled."); }
    private static void RevokeSessions(User user) { user.RefreshTokenHash = null; user.RefreshTokenCreatedAt = null; user.RefreshTokenExpiresAt = null; user.AuthorizationVersion = checked(user.AuthorizationVersion + 1); }

    private static readonly char[] Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
    private static string Base32Encode(byte[] data)
    {
        var output = new StringBuilder((data.Length * 8 + 4) / 5); int buffer = data[0], next = 1, bitsLeft = 8;
        while (bitsLeft > 0 || next < data.Length) { if (bitsLeft < 5) { if (next < data.Length) { buffer <<= 8; buffer |= data[next++] & 0xff; bitsLeft += 8; } else { buffer <<= 5 - bitsLeft; bitsLeft = 5; } } output.Append(Alphabet[(buffer >> (bitsLeft - 5)) & 0x1f]); bitsLeft -= 5; }
        return output.ToString();
    }
    private static byte[] Base32Decode(string value)
    {
        value = value.TrimEnd('=').ToUpperInvariant(); var output = new List<byte>(); int buffer = 0, bitsLeft = 0;
        foreach (var character in value) { var index = Array.IndexOf(Alphabet, character); if (index < 0) continue; buffer = (buffer << 5) | index; bitsLeft += 5; if (bitsLeft >= 8) { output.Add((byte)(buffer >> (bitsLeft - 8))); bitsLeft -= 8; } }
        return output.ToArray();
    }
}
