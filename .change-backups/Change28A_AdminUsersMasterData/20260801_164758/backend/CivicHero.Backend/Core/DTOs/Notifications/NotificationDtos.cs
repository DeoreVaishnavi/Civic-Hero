namespace CivicHero.Backend.Core.DTOs.Notifications;

public sealed class NotificationQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool UnreadOnly { get; set; }
    public string? Type { get; set; }
}

public sealed class NotificationResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public sealed class NotificationListResponse
{
    public IReadOnlyList<NotificationResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class NotificationPreferencesResponse
{
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool ComplaintUpdates { get; set; }
    public bool AssignmentUpdates { get; set; }
    public bool VerificationUpdates { get; set; }
    public bool DisputeUpdates { get; set; }
    public bool RewardUpdates { get; set; }
    public bool SecurityAlerts { get; set; }
}

public sealed class UpdateNotificationPreferencesRequest
{
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool ComplaintUpdates { get; set; } = true;
    public bool AssignmentUpdates { get; set; } = true;
    public bool VerificationUpdates { get; set; } = true;
    public bool DisputeUpdates { get; set; } = true;
    public bool RewardUpdates { get; set; } = true;
    public bool SecurityAlerts { get; set; } = true;
}

public sealed class BroadcastNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ActionUrl { get; set; }
}

public sealed record NotificationDispatchRequest(
    long UserId,
    string Title,
    string Message,
    string Type,
    string? ReferenceType = null,
    long? ReferenceId = null,
    string? ActionUrl = null,
    bool SendSignalR = true);
