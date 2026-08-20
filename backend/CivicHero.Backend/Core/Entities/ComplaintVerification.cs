using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;
namespace CivicHero.Backend.Core.Entities;
public sealed class ComplaintVerification : BaseEntity
{
    public long ComplaintId { get; set; }
    public long CitizenId { get; set; }
    public VerificationDecision Decision { get; set; } = VerificationDecision.Pending;
    public int? Rating { get; set; }
    public string? Remarks { get; set; }
    public decimal? SubmittedLatitude { get; set; }
    public decimal? SubmittedLongitude { get; set; }
    public double? DistanceMetres { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? ReminderSentAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public User Citizen { get; set; } = null!;
}
