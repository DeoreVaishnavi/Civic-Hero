namespace CivicHero.Backend.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.User;
        public int ReputationPoints { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ReputationLog> ReputationLogs { get; set; } = new List<ReputationLog>();
        public ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
        public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
        public ICollection<ComplaintUpdate> ComplaintUpdates { get; set; } = new List<ComplaintUpdate>();
    }
}