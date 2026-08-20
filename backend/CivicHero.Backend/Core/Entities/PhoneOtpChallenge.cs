using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class PhoneOtpChallenge : AuditableEntity
{
    public long UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int FailedAttempts { get; set; }
    public int MaximumAttempts { get; set; } = 5;
    public string? RequestedIpHash { get; set; }
    public string? ProviderMessageId { get; set; }

    public User User { get; set; } = null!;
}
