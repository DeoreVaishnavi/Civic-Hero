using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Notifications
{
    public class CreateNotificationDto
    {
        public int UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}