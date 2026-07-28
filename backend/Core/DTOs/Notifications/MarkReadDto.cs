namespace CivicHero.Backend.Core.DTOs.Notifications
{
    public class MarkReadDto
    {
        public int[] NotificationIds { get; set; } = Array.Empty<int>();
    }
}