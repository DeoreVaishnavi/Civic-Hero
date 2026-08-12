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
    public Department? Department { get; set; }
    public long? WardId { get; set; }
    public Ward? Ward { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Complaint> ComplaintsCreated { get; set; } = new List<Complaint>();
    public ICollection<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
    public ICollection<ReputationLog> ReputationLogs { get; set; } = new List<ReputationLog>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
