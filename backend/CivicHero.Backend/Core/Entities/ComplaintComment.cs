using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintComment : SoftDeleteEntity
{
    public long ComplaintId { get; set; }
    public long UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public CommentVisibility Visibility { get; set; } = CommentVisibility.Public;
    public CommentModerationStatus ModerationStatus { get; set; } = CommentModerationStatus.Visible;
    public string? ModerationReason { get; set; }
    public long? ModeratedByUserId { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? ModeratedByUser { get; set; }
}
