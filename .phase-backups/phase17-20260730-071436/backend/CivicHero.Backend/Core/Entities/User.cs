using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class User : SoftDeleteEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Citizen;
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public int AuthorizationVersion { get; set; } = 1;

    public string? EmailVerificationTokenHash { get; set; }
    public DateTimeOffset? EmailVerificationTokenExpiresAt { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenCreatedAt { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public Department? Department { get; set; }
    public Ward? Ward { get; set; }
    public ICollection<Complaint> CreatedComplaints { get; set; } = new List<Complaint>();
    public ICollection<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
    public ICollection<ComplaintAssignment> OfficerAssignments { get; set; } = new List<ComplaintAssignment>();
    public ICollection<ComplaintAssignment> AssignmentsCreated { get; set; } = new List<ComplaintAssignment>();
    public ICollection<ComplaintProgressUpdate> ProgressUpdates { get; set; } = new List<ComplaintProgressUpdate>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public NotificationPreference? NotificationPreference { get; set; }
    public ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
    public ICollection<ReputationLog> ReputationLogs { get; set; } = new List<ReputationLog>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<ComplaintVote> ComplaintVotes { get; set; } = new List<ComplaintVote>();
    public ICollection<ComplaintVerification> ComplaintVerifications { get; set; } = new List<ComplaintVerification>();
    public ICollection<ComplaintVerification> VerificationOverrides { get; set; } = new List<ComplaintVerification>();
}
