using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class AnonymousComplaintAccess : AuditableEntity
{
    public long ComplaintId { get; set; }
    public string TrackingTokenHash { get; set; } = string.Empty;
    public DateTimeOffset TrackingExpiresAt { get; set; }
    public string? ContactEmailProtected { get; set; }
    public string? ContactPhoneProtected { get; set; }
    public string ContactHint { get; set; } = string.Empty;
    public string CaptchaProvider { get; set; } = string.Empty;
    public string? SubmissionIpHash { get; set; }
    public decimal? CaptchaScore { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }

    public Complaint Complaint { get; set; } = null!;
}
