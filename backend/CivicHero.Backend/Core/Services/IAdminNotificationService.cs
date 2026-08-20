using CivicHero.Backend.Core.DTOs.Analytics;
using CivicHero.Backend.Core.DTOs.Notifications;

namespace CivicHero.Backend.Core.Services;

public interface IAdminNotificationService
{
    Task<IReadOnlyList<NotificationTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<NotificationTemplateResponse> CreateTemplateAsync(SaveNotificationTemplateRequest request, CancellationToken cancellationToken = default);
    Task<NotificationTemplateResponse> UpdateTemplateAsync(string key, SaveNotificationTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(string key, CancellationToken cancellationToken = default);
    Task<AdminBroadcastResult> BroadcastAsync(AdminBroadcastNotificationRequest request, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryListResponse> GetDeliveryLogsAsync(NotificationDeliveryQuery query, CancellationToken cancellationToken = default);
    Task<NotificationDeliverySummaryResponse> GetDeliverySummaryAsync(CancellationToken cancellationToken = default);
    Task<AnalyticsExportResult> ExportDeliveryLogsAsync(NotificationDeliveryQuery query, string format, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryResponse> RetryDeliveryAsync(long deliveryLogId, RetryNotificationDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduledBroadcastResponse>> GetSchedulesAsync(CancellationToken cancellationToken = default);
    Task CancelScheduleAsync(string id, CancellationToken cancellationToken = default);
    Task<int> ExecuteDueSchedulesAsync(CancellationToken cancellationToken = default);
}
