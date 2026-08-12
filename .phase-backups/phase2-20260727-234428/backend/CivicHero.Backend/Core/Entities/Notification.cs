using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class Notification : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
