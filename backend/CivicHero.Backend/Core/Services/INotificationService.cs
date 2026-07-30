using CivicHero.Backend.Core.DTOs.Notifications;

namespace CivicHero.Backend.Core.Services;

public interface INotificationService
{
    Task<NotificationListResponse> GetAsync(NotificationQuery query, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<NotificationResponse> MarkReadAsync(long id, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<NotificationPreferencesResponse> GetPreferencesAsync(CancellationToken cancellationToken = default);
    Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default);
    Task<NotificationResponse?> SendAsync(NotificationDispatchRequest request, CancellationToken cancellationToken = default);
    Task<int> BroadcastAsync(BroadcastNotificationRequest request, CancellationToken cancellationToken = default);
}
