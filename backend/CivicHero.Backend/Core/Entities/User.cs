using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a system user.
/// Every authenticated person in the application is stored here.
/// </summary>
public sealed class User : AuditableEntity
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool IsActive { get; set; } = true;

    public UserRole Role { get; set; } = UserRole.Citizen;

    public DateTime LastLoginUtc { get; set; }

    // Navigation Properties

    public ICollection<Complaint> Complaints { get; set; }
        = new List<Complaint>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();

    public ICollection<ComplaintVote> Votes { get; set; }
        = new List<ComplaintVote>();

    public ICollection<ReputationLog> ReputationLogs { get; set; }
        = new List<ReputationLog>();

    public ICollection<Redemption> Redemptions { get; set; }
        = new List<Redemption>();

    public ICollection<ChatSession> ChatSessions { get; set; }
        = new List<ChatSession>();
}