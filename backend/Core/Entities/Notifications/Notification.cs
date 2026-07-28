using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities.Notifications
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}