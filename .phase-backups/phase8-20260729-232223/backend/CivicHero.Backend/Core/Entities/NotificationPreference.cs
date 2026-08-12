using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class NotificationPreference : BaseEntity
{
    public long UserId { get; set; }
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool ComplaintUpdates { get; set; } = true;
    public bool AssignmentUpdates { get; set; } = true;
    public bool VerificationUpdates { get; set; } = true;
    public bool DisputeUpdates { get; set; } = true;
    public bool RewardUpdates { get; set; } = true;
    public bool SecurityAlerts { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
