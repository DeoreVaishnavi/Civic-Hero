namespace CivicHero.Backend.Core.DTOs.Notifications;

public sealed class SaveNotificationTemplateRequest
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public string? ActionUrlTemplate { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class NotificationTemplateResponse
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TitleTemplate { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public string? ActionUrlTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AdminBroadcastNotificationRequest
{
    public string? TemplateKey { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ActionUrl { get; set; }
    public string Type { get; set; } = "General";
    public bool SendInApp { get; set; } = true;
    public bool SendSignalR { get; set; } = true;
    public bool SendSms { get; set; }
    public bool SendEmail { get; set; }
    public DateTimeOffset? ScheduledFor { get; set; }
}

public sealed class AdminBroadcastResult
{
    public string BroadcastId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TargetUsers { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public int SkippedDeliveries { get; set; }
    public DateTimeOffset? ScheduledFor { get; set; }
}

public sealed class NotificationDeliveryQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Channel { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string? BroadcastId { get; set; }
}

public sealed class NotificationDeliveryResponse
{
    public long Id { get; set; }
    public string BroadcastId { get; set; } = string.Empty;
    public long RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public string? ActionUrl { get; set; }
    public string? TemplateKey { get; set; }
    public int AttemptNumber { get; set; }
    public long? RetriedFromLogId { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class NotificationDeliveryListResponse
{
    public IReadOnlyList<NotificationDeliveryResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class NotificationDeliverySummaryResponse
{
    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int NotConfigured { get; set; }
    public IReadOnlyDictionary<string, int> ByChannel { get; set; } = new Dictionary<string, int>();
}

public sealed class RetryNotificationDeliveryRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class ScheduledBroadcastResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Role { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int RecipientCount { get; set; }
    public string? Error { get; set; }
}
