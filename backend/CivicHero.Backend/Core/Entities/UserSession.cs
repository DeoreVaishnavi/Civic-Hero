using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class UserSession : AuditableEntity
{
    public long UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public int AuthorizationVersion { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string DeviceLabel { get; set; } = "Unknown device";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public User User { get; set; } = null!;
}
