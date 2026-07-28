using CivicHero.Backend.Core.DTOs.Notifications;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(SendNotificationDto dto);
        Task<List<NotificationDto>> GetNotificationsForUserAsync(int userId, bool includeRead = false);
        Task MarkAsReadAsync(int userId, MarkReadDto dto);
        Task<int> GetUnreadCountAsync(int userId);
    }
}