
namespace CivicHero.Backend.Core.DTOs.Auth;

public sealed record TwoFactorStatusResponse(bool Enabled, bool SetupPending, DateTimeOffset? EnabledAtUtc, int RecoveryCodesRemaining);
public sealed record TwoFactorSetupResponse(string ManualEntryKey, string OtpAuthUri, string Issuer, string AccountName);
public sealed record TwoFactorEnableResponse(bool Enabled, IReadOnlyList<string> RecoveryCodes, bool RequiresSignInAgain);
