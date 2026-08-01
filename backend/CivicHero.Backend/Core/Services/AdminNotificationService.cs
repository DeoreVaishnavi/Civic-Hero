using System.Text.Json;
using System.Text.RegularExpressions;
using CivicHero.Backend.Core.DTOs.Analytics;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class AdminNotificationService : IAdminNotificationService
{
    private const string TemplatePrefix = "NotificationTemplate:";
    private const string SchedulePrefix = "NotificationSchedule:";
    private const string SettingsGroup = "Notifications";
    private const string DeliveryAction = "NotificationDelivery";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex TemplateKeyPattern = new("^[a-z0-9][a-z0-9_-]{1,59}$", RegexOptions.Compiled);

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly ISmsSender _smsSender;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<AdminNotificationService> _logger;

    public AdminNotificationService(
        CivicDbContext db,
        ICurrentUserService currentUser,
        INotificationService notifications,
        ISmsSender smsSender,
        IHubContext<NotificationHub> hub,
        ILogger<AdminNotificationService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _smsSender = smsSender;
        _hub = hub;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        var settings = await _db.SystemSettings.AsNoTracking()
            .Where(entity => entity.Group == SettingsGroup && entity.Key.StartsWith(TemplatePrefix))
            .OrderBy(entity => entity.Key)
            .ToListAsync(cancellationToken);

        return settings.Select(TryMapTemplate).Where(item => item is not null).Cast<NotificationTemplateResponse>().ToList();
    }

    public async Task<NotificationTemplateResponse> CreateTemplateAsync(SaveNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var key = NormalizeTemplateKey(request.Key);
        var settingKey = TemplatePrefix + key;
        if (await _db.SystemSettings.AnyAsync(entity => entity.Key == settingKey, cancellationToken))
            throw new BusinessRuleViolationException("A notification template with this key already exists.");

        var now = DateTimeOffset.UtcNow;
        var response = NormalizeTemplate(request, key, now);
        _db.SystemSettings.Add(new SystemSetting
        {
            Key = settingKey,
            Value = JsonSerializer.Serialize(response, JsonOptions),
            ValueType = "Json",
            Description = $"Notification template: {response.Name}",
            Group = SettingsGroup,
            IsPublic = false,
            IsSensitive = false,
            CreatedAt = now,
            UpdatedAt = now,
            UpdatedByUserId = adminId
        });
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "NotificationTemplateCreated", "SystemSetting", settingKey, null, response));
        await _db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<NotificationTemplateResponse> UpdateTemplateAsync(string key, SaveNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var normalizedKey = NormalizeTemplateKey(key);
        var settingKey = TemplatePrefix + normalizedKey;
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(entity => entity.Key == settingKey, cancellationToken)
            ?? throw new NotFoundException("Notification template was not found.");
        var previous = TryMapTemplate(setting);
        var response = NormalizeTemplate(request, normalizedKey, DateTimeOffset.UtcNow);
        setting.Value = JsonSerializer.Serialize(response, JsonOptions);
        setting.Description = $"Notification template: {response.Name}";
        setting.UpdatedAt = response.UpdatedAt;
        setting.UpdatedByUserId = adminId;
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "NotificationTemplateUpdated", "SystemSetting", settingKey, previous, response));
        await _db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task DeleteTemplateAsync(string key, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var normalizedKey = NormalizeTemplateKey(key);
        var settingKey = TemplatePrefix + normalizedKey;
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(entity => entity.Key == settingKey, cancellationToken)
            ?? throw new NotFoundException("Notification template was not found.");
        var previous = TryMapTemplate(setting);
        _db.SystemSettings.Remove(setting);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "NotificationTemplateDeleted", "SystemSetting", settingKey, previous, null));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminBroadcastResult> BroadcastAsync(AdminBroadcastNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var actorEmail = _currentUser.Email;
        var now = DateTimeOffset.UtcNow;
        if (request.ScheduledFor.HasValue && request.ScheduledFor.Value > now.AddSeconds(30))
        {
            var audience = await ResolveAudienceAsync(request, cancellationToken);
            return await CreateScheduleAsync(request, adminId, actorEmail, audience, cancellationToken);
        }

        return await ExecuteBroadcastInternalAsync(request, Guid.NewGuid().ToString("N"), adminId, actorEmail, cancellationToken);
    }

    public async Task<NotificationDeliveryListResponse> GetDeliveryLogsAsync(NotificationDeliveryQuery query, CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        var materialized = await LoadFilteredDeliveryLogsAsync(query, cancellationToken);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        return new NotificationDeliveryListResponse
        {
            Items = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = materialized.Count
        };
    }

    public async Task<AnalyticsExportResult> ExportDeliveryLogsAsync(NotificationDeliveryQuery query, string format, CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        var items = (await LoadFilteredDeliveryLogsAsync(query, cancellationToken)).Take(5000).ToList();
        var table = new ReportTable(
            "CivicHero notification delivery report",
            ["Time", "Broadcast ID", "Recipient", "Email", "Channel", "Provider", "Status", "Title", "Attempt", "Error"],
            items.Select(item => (IReadOnlyList<object?>)new object?[]
            { item.CreatedAt, item.BroadcastId, item.RecipientName, item.RecipientEmail, item.Channel, item.Provider, item.Status, item.Title, item.AttemptNumber, item.Error }).ToList(),
            $"Generated {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC. Maximum 5,000 filtered records.");
        return ReportDocumentBuilder.Build(table, format, $"civichero-notification-deliveries-{DateTime.UtcNow:yyyyMMdd-HHmm}");
    }

    public async Task<NotificationDeliverySummaryResponse> GetDeliverySummaryAsync(CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        var audits = await _db.AuditLogs.AsNoTracking()
            .Where(entity => entity.Action == DeliveryAction)
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
        var items = audits.Select(TryMapDelivery).Where(item => item is not null).Cast<NotificationDeliveryResponse>().ToList();
        return new NotificationDeliverySummaryResponse
        {
            Total = items.Count,
            Sent = items.Count(item => item.Status == DeliveryStatuses.Sent),
            Failed = items.Count(item => item.Status == DeliveryStatuses.Failed),
            Skipped = items.Count(item => item.Status == DeliveryStatuses.Skipped),
            NotConfigured = items.Count(item => item.Status == DeliveryStatuses.NotConfigured),
            ByChannel = items.GroupBy(item => item.Channel).ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task<NotificationDeliveryResponse> RetryDeliveryAsync(long deliveryLogId, RetryNotificationDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var audit = await _db.AuditLogs.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == deliveryLogId && entity.Action == DeliveryAction, cancellationToken)
            ?? throw new NotFoundException("Notification delivery record was not found.");
        var original = DeserializeDelivery(audit.NewValuesJson)
            ?? throw new BusinessRuleViolationException("The delivery record cannot be retried because its payload is invalid.");
        if (original.Status == DeliveryStatuses.Sent)
            throw new BusinessRuleViolationException("A successful delivery cannot be retried.");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == original.RecipientUserId && entity.IsActive && !entity.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("The original recipient is no longer active.");
        var preference = await _db.NotificationPreferences.AsNoTracking().FirstOrDefaultAsync(entity => entity.UserId == user.Id, cancellationToken);
        var actorEmail = _currentUser.Email;
        var retry = await DispatchSingleChannelAsync(
            user,
            preference,
            original.Channel,
            original.Title,
            original.Message,
            original.Type,
            original.ActionUrl,
            original.TemplateKey,
            original.BroadcastId,
            adminId,
            actorEmail,
            original.AttemptNumber + 1,
            deliveryLogId,
            cancellationToken);
        retry.NewValuesJson = MergeRetryReason(retry.NewValuesJson, request.Reason.Trim());
        _db.AuditLogs.Add(retry);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "NotificationDeliveryRetried", "AuditLog", deliveryLogId.ToString(), new { original.Status }, new { retryChannel = original.Channel, reason = request.Reason.Trim() }));
        await _db.SaveChangesAsync(cancellationToken);
        return TryMapDelivery(retry) ?? throw new InvalidOperationException("Retry delivery record could not be created.");
    }

    public async Task<IReadOnlyList<ScheduledBroadcastResponse>> GetSchedulesAsync(CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        var settings = await _db.SystemSettings.AsNoTracking()
            .Where(entity => entity.Group == SettingsGroup && entity.Key.StartsWith(SchedulePrefix))
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
        return settings.Select(TryMapSchedule).Where(item => item is not null).Cast<ScheduledBroadcastResponse>().ToList();
    }

    public async Task CancelScheduleAsync(string id, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var normalizedId = NormalizeScheduleId(id);
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(entity => entity.Key == SchedulePrefix + normalizedId, cancellationToken)
            ?? throw new NotFoundException("Scheduled broadcast was not found.");
        var state = DeserializeSchedule(setting.Value)
            ?? throw new BusinessRuleViolationException("Scheduled broadcast data is invalid.");
        if (state.Status != ScheduleStatuses.Pending)
            throw new BusinessRuleViolationException("Only a pending broadcast can be cancelled.");
        state.Status = ScheduleStatuses.Cancelled;
        state.CompletedAt = DateTimeOffset.UtcNow;
        setting.Value = JsonSerializer.Serialize(state, JsonOptions);
        setting.UpdatedAt = state.CompletedAt.Value;
        setting.UpdatedByUserId = adminId;
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "NotificationScheduleCancelled", "SystemSetting", setting.Key, null, new { state.Id, state.ScheduledFor }));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExecuteDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var settings = await _db.SystemSettings
            .Where(entity => entity.Group == SettingsGroup && entity.Key.StartsWith(SchedulePrefix))
            .ToListAsync(cancellationToken);
        var executed = 0;

        foreach (var setting in settings)
        {
            var state = DeserializeSchedule(setting.Value);
            if (state is null || state.Status != ScheduleStatuses.Pending || state.ScheduledFor > now) continue;
            state.Status = ScheduleStatuses.Processing;
            state.LastAttemptAt = now;
            setting.Value = JsonSerializer.Serialize(state, JsonOptions);
            setting.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var result = await ExecuteBroadcastInternalAsync(state.Request, state.Id, state.CreatedByUserId, state.CreatedByEmail, cancellationToken);
                state.Status = ScheduleStatuses.Completed;
                state.CompletedAt = DateTimeOffset.UtcNow;
                state.AudienceType = result.AudienceType;
                state.AudienceLabel = result.AudienceLabel;
                state.RecipientCount = result.TargetUsers;
                state.Error = null;
                executed++;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Scheduled notification broadcast {BroadcastId} failed.", state.Id);
                state.Status = ScheduleStatuses.Failed;
                state.CompletedAt = DateTimeOffset.UtcNow;
                state.Error = Truncate(exception.Message, 1000);
            }

            setting.Value = JsonSerializer.Serialize(state, JsonOptions);
            setting.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return executed;
    }

    private async Task<AdminBroadcastResult> CreateScheduleAsync(
        AdminBroadcastNotificationRequest request,
        long adminId,
        string? actorEmail,
        ResolvedAudience audience,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var state = new ScheduledBroadcastState
        {
            Id = id,
            Status = ScheduleStatuses.Pending,
            Request = request,
            AudienceType = audience.Type,
            AudienceLabel = audience.Label,
            ScheduledFor = request.ScheduledFor!.Value,
            CreatedAt = now,
            CreatedByUserId = adminId,
            CreatedByEmail = actorEmail,
            RecipientCount = audience.Users.Count
        };
        _db.SystemSettings.Add(new SystemSetting
        {
            Key = SchedulePrefix + id,
            Value = JsonSerializer.Serialize(state, JsonOptions),
            ValueType = "Json",
            Description = $"Scheduled notification broadcast for {state.ScheduledFor:u}",
            Group = SettingsGroup,
            IsPublic = false,
            IsSensitive = false,
            CreatedAt = now,
            UpdatedAt = now,
            UpdatedByUserId = adminId
        });
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "NotificationBroadcastScheduled", "SystemSetting", SchedulePrefix + id, null, new
        {
            id,
            state.ScheduledFor,
            audienceType = audience.Type,
            audienceLabel = audience.Label,
            previewRecipients = audience.Users.Count,
            request.Role,
            request.DepartmentId,
            request.WardId,
            request.GroupIdentifier,
            selectedUserIds = request.SelectedUserIds,
            request.TemplateKey
        }));
        await _db.SaveChangesAsync(cancellationToken);
        return new AdminBroadcastResult
        {
            BroadcastId = id,
            Status = ScheduleStatuses.Pending,
            AudienceType = audience.Type,
            AudienceLabel = audience.Label,
            TargetUsers = audience.Users.Count,
            ScheduledFor = state.ScheduledFor
        };
    }

    private async Task<AdminBroadcastResult> ExecuteBroadcastInternalAsync(
        AdminBroadcastNotificationRequest request,
        string broadcastId,
        long actorId,
        string? actorEmail,
        CancellationToken cancellationToken)
    {
        var content = await ResolveContentAsync(request, cancellationToken);
        var audience = await ResolveAudienceAsync(request, cancellationToken);
        var users = audience.Users;
        var userIds = users.Select(entity => entity.Id).ToList();
        var preferences = await _db.NotificationPreferences.AsNoTracking()
            .Where(entity => userIds.Contains(entity.UserId))
            .ToDictionaryAsync(entity => entity.UserId, cancellationToken);

        var sent = 0;
        var failed = 0;
        var skipped = 0;
        foreach (var user in users)
        {
            preferences.TryGetValue(user.Id, out var preference);
            var title = Render(content.Title, user);
            var message = Render(content.Message, user);
            var actionUrl = string.IsNullOrWhiteSpace(content.ActionUrl) ? null : Render(content.ActionUrl, user);

            var logs = await DispatchRequestedChannelsAsync(
                user,
                preference,
                request,
                title,
                message,
                content.Type,
                actionUrl,
                content.TemplateKey,
                broadcastId,
                actorId,
                actorEmail,
                cancellationToken);
            foreach (var log in logs)
            {
                var payload = DeserializeDelivery(log.NewValuesJson);
                if (payload?.Status == DeliveryStatuses.Sent) sent++;
                else if (payload?.Status == DeliveryStatuses.Skipped) skipped++;
                else failed++;
                _db.AuditLogs.Add(log);
            }
        }
        _db.AuditLogs.Add(CreateAdminAudit(actorId, "NotificationBroadcastCompleted", "NotificationBroadcast", broadcastId, null, new
        {
            targetUsers = users.Count,
            sent,
            failed,
            skipped,
            audienceType = audience.Type,
            audienceLabel = audience.Label,
            request.Role,
            request.DepartmentId,
            request.WardId,
            request.GroupIdentifier,
            selectedUserIds = request.SelectedUserIds,
            content.TemplateKey
        }));
        await _db.SaveChangesAsync(cancellationToken);
        return new AdminBroadcastResult
        {
            BroadcastId = broadcastId,
            Status = ScheduleStatuses.Completed,
            AudienceType = audience.Type,
            AudienceLabel = audience.Label,
            TargetUsers = users.Count,
            SuccessfulDeliveries = sent,
            FailedDeliveries = failed,
            SkippedDeliveries = skipped
        };
    }

    private async Task<IReadOnlyList<AuditLog>> DispatchRequestedChannelsAsync(
        User user,
        NotificationPreference? preference,
        AdminBroadcastNotificationRequest request,
        string title,
        string message,
        string type,
        string? actionUrl,
        string? templateKey,
        string broadcastId,
        long actorId,
        string? actorEmail,
        CancellationToken cancellationToken)
    {
        var logs = new List<AuditLog>();
        if (request.SendInApp)
        {
            var enabled = preference?.InAppEnabled ?? true;
            if (!enabled)
            {
                logs.Add(CreateDeliveryAudit(user, "InApp", DeliveryStatuses.Skipped, "User preference disabled in-app notifications.", null, null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, 1, null));
            }
            else
            {
                try
                {
                    var created = await _notifications.SendAsync(new NotificationDispatchRequest(user.Id, title, message, type, "Broadcast", null, actionUrl, false), cancellationToken);
                    var status = created is null ? DeliveryStatuses.Skipped : DeliveryStatuses.Sent;
                    logs.Add(CreateDeliveryAudit(user, "InApp", status, created is null ? "Notification category preference disabled delivery." : null, "CivicHero", created?.Id.ToString(), title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, 1, null));
                }
                catch (Exception exception)
                {
                    logs.Add(CreateDeliveryAudit(user, "InApp", DeliveryStatuses.Failed, exception.Message, "CivicHero", null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, 1, null));
                }
            }
        }

        if (request.SendSignalR)
            logs.Add(await DispatchSignalRAsync(user, preference, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, 1, null, cancellationToken));
        if (request.SendSms)
            logs.Add(await DispatchSmsAsync(user, preference, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, 1, null, cancellationToken));
        if (request.SendEmail)
            logs.Add(DispatchEmailUnavailable(user, preference, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, 1, null));
        return logs;
    }

    private async Task<AuditLog> DispatchSingleChannelAsync(
        User user,
        NotificationPreference? preference,
        string channel,
        string title,
        string message,
        string type,
        string? actionUrl,
        string? templateKey,
        string broadcastId,
        long actorId,
        string? actorEmail,
        int attemptNumber,
        long? retriedFromLogId,
        CancellationToken cancellationToken)
    {
        if (channel.Equals("InApp", StringComparison.OrdinalIgnoreCase))
        {
            if (!(preference?.InAppEnabled ?? true))
                return CreateDeliveryAudit(user, "InApp", DeliveryStatuses.Skipped, "User preference disabled in-app notifications.", null, null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
            try
            {
                var created = await _notifications.SendAsync(new NotificationDispatchRequest(user.Id, title, message, type, "BroadcastRetry", null, actionUrl, false), cancellationToken);
                return CreateDeliveryAudit(user, "InApp", created is null ? DeliveryStatuses.Skipped : DeliveryStatuses.Sent, created is null ? "Notification category preference disabled delivery." : null, "CivicHero", created?.Id.ToString(), title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
            }
            catch (Exception exception)
            {
                return CreateDeliveryAudit(user, "InApp", DeliveryStatuses.Failed, exception.Message, "CivicHero", null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
            }
        }
        if (channel.Equals("SignalR", StringComparison.OrdinalIgnoreCase))
            return await DispatchSignalRAsync(user, preference, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId, cancellationToken);
        if (channel.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            return await DispatchSmsAsync(user, preference, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId, cancellationToken);
        if (channel.Equals("Email", StringComparison.OrdinalIgnoreCase))
            return DispatchEmailUnavailable(user, preference, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        throw new BusinessRuleViolationException("The stored notification channel is not supported.");
    }

    private async Task<AuditLog> DispatchSignalRAsync(User user, NotificationPreference? preference, string title, string message, string type, string? actionUrl, string? templateKey, string broadcastId, long actorId, string? actorEmail, int attemptNumber, long? retriedFromLogId, CancellationToken cancellationToken)
    {
        if (!(preference?.InAppEnabled ?? true))
            return CreateDeliveryAudit(user, "SignalR", DeliveryStatuses.Skipped, "User preference disabled real-time notifications.", "SignalR", null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        try
        {
            await _hub.Clients.Group($"user_{user.Id}").SendAsync("notificationReceived", new { title, message, type, actionUrl, createdAt = DateTimeOffset.UtcNow }, cancellationToken);
            return CreateDeliveryAudit(user, "SignalR", DeliveryStatuses.Sent, null, "SignalR", null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        }
        catch (Exception exception)
        {
            return CreateDeliveryAudit(user, "SignalR", DeliveryStatuses.Failed, exception.Message, "SignalR", null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        }
    }

    private async Task<AuditLog> DispatchSmsAsync(User user, NotificationPreference? preference, string title, string message, string type, string? actionUrl, string? templateKey, string broadcastId, long actorId, string? actorEmail, int attemptNumber, long? retriedFromLogId, CancellationToken cancellationToken)
    {
        if (!(preference?.SmsEnabled ?? false))
            return CreateDeliveryAudit(user, "SMS", DeliveryStatuses.Skipped, "User preference disabled SMS notifications.", null, null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        if (string.IsNullOrWhiteSpace(user.Phone))
            return CreateDeliveryAudit(user, "SMS", DeliveryStatuses.Skipped, "Recipient does not have a phone number.", null, null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        var result = await _smsSender.SendAsync(user.Phone, $"{title}: {message}", cancellationToken);
        return CreateDeliveryAudit(user, "SMS", result.Success ? DeliveryStatuses.Sent : DeliveryStatuses.Failed, result.Error, result.Provider, result.ProviderMessageId, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
    }

    private AuditLog DispatchEmailUnavailable(User user, NotificationPreference? preference, string title, string message, string type, string? actionUrl, string? templateKey, string broadcastId, long actorId, string? actorEmail, int attemptNumber, long? retriedFromLogId)
    {
        if (!(preference?.EmailEnabled ?? true))
            return CreateDeliveryAudit(user, "Email", DeliveryStatuses.Skipped, "User preference disabled email notifications.", null, null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
        return CreateDeliveryAudit(user, "Email", DeliveryStatuses.NotConfigured, "No production email sender is implemented in the current CivicHero project.", "NotConfigured", null, title, message, type, actionUrl, templateKey, broadcastId, actorId, actorEmail, attemptNumber, retriedFromLogId);
    }

    private async Task<ResolvedBroadcastContent> ResolveContentAsync(AdminBroadcastNotificationRequest request, CancellationToken cancellationToken)
    {
        NotificationTemplateResponse? template = null;
        if (!string.IsNullOrWhiteSpace(request.TemplateKey))
        {
            var key = NormalizeTemplateKey(request.TemplateKey);
            var setting = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(entity => entity.Key == TemplatePrefix + key, cancellationToken)
                ?? throw new NotFoundException("Selected notification template was not found.");
            template = TryMapTemplate(setting) ?? throw new BusinessRuleViolationException("Selected notification template is invalid.");
            if (!template.IsActive) throw new BusinessRuleViolationException("Selected notification template is inactive.");
        }

        var title = string.IsNullOrWhiteSpace(request.Title) ? template?.TitleTemplate : request.Title.Trim();
        var message = string.IsNullOrWhiteSpace(request.Message) ? template?.MessageTemplate : request.Message.Trim();
        var type = string.IsNullOrWhiteSpace(request.Type) || request.Type.Equals("General", StringComparison.OrdinalIgnoreCase)
            ? template?.Type ?? "General"
            : request.Type.Trim();
        var actionUrl = string.IsNullOrWhiteSpace(request.ActionUrl) ? template?.ActionUrlTemplate : request.ActionUrl.Trim();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            throw new BusinessRuleViolationException("Broadcast title and message are required when no complete template is selected.");
        return new ResolvedBroadcastContent(title, message, CanonicalNotificationType(type), actionUrl, template?.Key);
    }

    private async Task<ResolvedAudience> ResolveAudienceAsync(
        AdminBroadcastNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var audienceType = CanonicalAudienceType(request);
        request.AudienceType = audienceType;

        IQueryable<User> usersQuery = _db.Users.AsNoTracking()
            .Where(entity => entity.IsActive && entity.IsEmailVerified && !entity.IsDeleted);
        string audienceLabel;

        switch (audienceType)
        {
            case "All":
                request.Role = null;
                request.DepartmentId = null;
                request.WardId = null;
                request.GroupIdentifier = null;
                request.SelectedUserIds = [];
                audienceLabel = "All active users";
                break;

            case "Role":
                if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
                    throw new BusinessRuleViolationException("Select a valid role for the broadcast audience.");
                request.Role = role.ToString();
                request.DepartmentId = null;
                request.WardId = null;
                request.GroupIdentifier = null;
                request.SelectedUserIds = [];
                usersQuery = usersQuery.Where(entity => entity.Role == role);
                audienceLabel = $"{role} role";
                break;

            case "Department":
            {
                var departmentId = request.DepartmentId
                    ?? throw new BusinessRuleViolationException("Select a department group for the broadcast audience.");
                var department = await _db.Departments.AsNoTracking()
                    .FirstOrDefaultAsync(entity => entity.Id == departmentId && entity.IsActive && !entity.IsDeleted, cancellationToken)
                    ?? throw new NotFoundException("Selected department group was not found or is inactive.");
                var citizenIds = _db.Complaints.AsNoTracking()
                    .Where(entity => entity.DepartmentId == departmentId && !entity.IsDeleted)
                    .Select(entity => entity.CitizenId);
                usersQuery = usersQuery.Where(entity =>
                    entity.DepartmentId == departmentId ||
                    (entity.Role == UserRole.Citizen && citizenIds.Contains(entity.Id)));
                request.Role = null;
                request.WardId = null;
                request.GroupIdentifier = null;
                request.SelectedUserIds = [];
                audienceLabel = $"Department group: {department.Name}";
                break;
            }

            case "Ward":
            {
                var wardId = request.WardId
                    ?? throw new BusinessRuleViolationException("Select a ward for the broadcast audience.");
                var ward = await _db.Wards.AsNoTracking()
                    .FirstOrDefaultAsync(entity => entity.Id == wardId && entity.IsActive && !entity.IsDeleted, cancellationToken)
                    ?? throw new NotFoundException("Selected ward was not found or is inactive.");
                var citizenIds = _db.Complaints.AsNoTracking()
                    .Where(entity => entity.WardId == wardId && !entity.IsDeleted)
                    .Select(entity => entity.CitizenId);
                usersQuery = usersQuery.Where(entity =>
                    entity.WardId == wardId ||
                    (entity.Role == UserRole.Citizen && citizenIds.Contains(entity.Id)));
                request.Role = null;
                request.DepartmentId = null;
                request.GroupIdentifier = null;
                request.SelectedUserIds = [];
                audienceLabel = $"Ward community: {ward.Name}";
                break;
            }

            case "Group":
            {
                var groupIdentifier = CanonicalGroupIdentifier(request.GroupIdentifier);
                request.Role = null;
                request.DepartmentId = null;
                request.WardId = null;
                request.GroupIdentifier = groupIdentifier;
                request.SelectedUserIds = [];

                switch (groupIdentifier)
                {
                    case "AllStaff":
                        usersQuery = usersQuery.Where(entity => entity.Role != UserRole.Citizen);
                        audienceLabel = "Group: All staff";
                        break;
                    case "FieldOperations":
                        usersQuery = usersQuery.Where(entity =>
                            entity.Role == UserRole.Officer || entity.Role == UserRole.Supervisor);
                        audienceLabel = "Group: Field operations";
                        break;
                    case "Administrators":
                        usersQuery = usersQuery.Where(entity =>
                            entity.Role == UserRole.Admin || entity.Role == UserRole.SuperAdmin);
                        audienceLabel = "Group: Administrators";
                        break;
                    case "CitizensWithActiveComplaints":
                    {
                        var inactiveStatuses = new[]
                        {
                            ComplaintStatus.Merged,
                            ComplaintStatus.Closed,
                            ComplaintStatus.ClosedAuto,
                            ComplaintStatus.ClosedFraud,
                            ComplaintStatus.Withdrawn
                        };
                        var citizenIds = _db.Complaints.AsNoTracking()
                            .Where(entity => !entity.IsDeleted && !inactiveStatuses.Contains(entity.Status))
                            .Select(entity => entity.CitizenId);
                        usersQuery = usersQuery.Where(entity =>
                            entity.Role == UserRole.Citizen && citizenIds.Contains(entity.Id));
                        audienceLabel = "Group: Citizens with active complaints";
                        break;
                    }
                    default:
                        throw new BusinessRuleViolationException("Notification group is not supported.");
                }
                break;
            }

            case "SelectedUsers":
            {
                var selectedIds = (request.SelectedUserIds ?? [])
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();
                if (selectedIds.Count == 0)
                    throw new BusinessRuleViolationException("Select at least one user for the broadcast audience.");
                if (selectedIds.Count > 200)
                    throw new BusinessRuleViolationException("A broadcast can target at most 200 selected users.");

                request.Role = null;
                request.DepartmentId = null;
                request.WardId = null;
                request.GroupIdentifier = null;
                request.SelectedUserIds = selectedIds;
                usersQuery = usersQuery.Where(entity => selectedIds.Contains(entity.Id));
                audienceLabel = selectedIds.Count == 1 ? "1 selected user" : $"{selectedIds.Count} selected users";
                break;
            }

            default:
                throw new BusinessRuleViolationException("Notification audience type is not supported.");
        }

        var users = await usersQuery.OrderBy(entity => entity.Id).ToListAsync(cancellationToken);
        if (audienceType == "SelectedUsers")
        {
            var missingIds = request.SelectedUserIds.Except(users.Select(user => user.Id)).Take(10).ToList();
            if (missingIds.Count > 0)
                throw new BusinessRuleViolationException(
                    $"Selected users are missing, inactive, unverified, or deleted: {string.Join(", ", missingIds)}.");
        }

        return new ResolvedAudience(audienceType, audienceLabel, users);
    }

    private static string CanonicalAudienceType(AdminBroadcastNotificationRequest request)
    {
        var value = string.IsNullOrWhiteSpace(request.AudienceType)
            ? (string.IsNullOrWhiteSpace(request.Role) ? "All" : "Role")
            : request.AudienceType.Trim();
        return value.ToLowerInvariant() switch
        {
            "all" => "All",
            "role" => "Role",
            "department" => "Department",
            "ward" => "Ward",
            "group" => "Group",
            "selectedusers" or "selected-users" or "users" => "SelectedUsers",
            _ => throw new BusinessRuleViolationException("Notification audience type is not supported.")
        };
    }

    private static string CanonicalGroupIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleViolationException("Select a predefined notification group.");
        return value.Trim().ToLowerInvariant() switch
        {
            "allstaff" or "all-staff" => "AllStaff",
            "fieldoperations" or "field-operations" => "FieldOperations",
            "administrators" or "admins" => "Administrators",
            "citizenswithactivecomplaints" or "citizens-with-active-complaints" => "CitizensWithActiveComplaints",
            _ => throw new BusinessRuleViolationException("Notification group is not supported.")
        };
    }

    private static string DescribeAudienceRequest(AdminBroadcastNotificationRequest request)
    {
        var type = CanonicalAudienceType(request);
        return type switch
        {
            "Role" => $"{request.Role ?? "Unknown"} role",
            "Department" => request.DepartmentId.HasValue ? $"Department group #{request.DepartmentId.Value}" : "Department group",
            "Ward" => request.WardId.HasValue ? $"Ward community #{request.WardId.Value}" : "Ward community",
            "Group" => request.GroupIdentifier switch
            {
                "AllStaff" => "Group: All staff",
                "FieldOperations" => "Group: Field operations",
                "Administrators" => "Group: Administrators",
                "CitizensWithActiveComplaints" => "Group: Citizens with active complaints",
                _ => "Notification group"
            },
            "SelectedUsers" => (request.SelectedUserIds?.Count ?? 0) == 1
                ? "1 selected user"
                : $"{request.SelectedUserIds?.Count ?? 0} selected users",
            _ => "All active users"
        };
    }

    private static string Render(string template, User user) => template
        .Replace("{{FullName}}", user.FullName, StringComparison.OrdinalIgnoreCase)
        .Replace("{{Email}}", user.Email, StringComparison.OrdinalIgnoreCase)
        .Replace("{{Role}}", user.Role.ToString(), StringComparison.OrdinalIgnoreCase)
        .Replace("{{UserId}}", user.Id.ToString(), StringComparison.OrdinalIgnoreCase);

    private static AuditLog CreateDeliveryAudit(User recipient, string channel, string status, string? error, string? provider, string? providerMessageId, string title, string message, string type, string? actionUrl, string? templateKey, string broadcastId, long actorId, string? actorEmail, int attemptNumber, long? retriedFromLogId)
    {
        var payload = new DeliveryRecord
        {
            BroadcastId = broadcastId,
            RecipientUserId = recipient.Id,
            RecipientName = recipient.FullName,
            RecipientEmail = recipient.Email,
            Channel = channel,
            Status = status,
            Provider = provider,
            ProviderMessageId = providerMessageId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            TemplateKey = templateKey,
            AttemptNumber = attemptNumber,
            RetriedFromLogId = retriedFromLogId,
            Error = Truncate(error, 1000)
        };
        return new AuditLog
        {
            UserId = actorId,
            UserEmail = actorEmail,
            UserRole = "Admin",
            Action = DeliveryAction,
            EntityName = "NotificationDelivery",
            EntityId = broadcastId,
            NewValuesJson = JsonSerializer.Serialize(payload, JsonOptions),
            CorrelationId = broadcastId,
            Severity = status is DeliveryStatuses.Failed or DeliveryStatuses.NotConfigured ? "Warning" : "Information",
            Success = status == DeliveryStatuses.Sent,
            HttpStatusCode = status switch
            {
                DeliveryStatuses.Sent => 200,
                DeliveryStatuses.Skipped => 204,
                DeliveryStatuses.NotConfigured => 501,
                _ => 503
            },
            ErrorMessage = Truncate(error, 1000),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private AuditLog CreateAdminAudit(long adminId, string action, string entityName, string entityId, object? oldValue, object? newValue) => new()
    {
        UserId = adminId,
        UserEmail = _currentUser.UserId == adminId ? _currentUser.Email : null,
        UserRole = _currentUser.UserId == adminId ? _currentUser.Role : "System",
        Action = action,
        EntityName = entityName,
        EntityId = entityId,
        OldValuesJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
        NewValuesJson = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
        Severity = "Information",
        Success = true,
        HttpStatusCode = 200,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static NotificationTemplateResponse NormalizeTemplate(SaveNotificationTemplateRequest request, string key, DateTimeOffset updatedAt) => new()
    {
        Key = key,
        Name = request.Name.Trim(),
        TitleTemplate = request.TitleTemplate.Trim(),
        MessageTemplate = request.MessageTemplate.Trim(),
        Type = CanonicalNotificationType(request.Type),
        ActionUrlTemplate = string.IsNullOrWhiteSpace(request.ActionUrlTemplate) ? null : request.ActionUrlTemplate.Trim(),
        IsActive = request.IsActive,
        UpdatedAt = updatedAt
    };

    private static string CanonicalNotificationType(string type)
    {
        if (!Enum.TryParse<NotificationType>(type, true, out var parsed))
            throw new BusinessRuleViolationException("Notification type is not supported.");
        return parsed.ToString();
    }

    private static string NormalizeTemplateKey(string key)
    {
        var value = key.Trim().ToLowerInvariant();
        if (!TemplateKeyPattern.IsMatch(value))
            throw new BusinessRuleViolationException("Template key must contain 2-60 lowercase letters, numbers, hyphens, or underscores.");
        return value;
    }

    private static string NormalizeScheduleId(string id)
    {
        var value = id.Trim().ToLowerInvariant();
        if (value.Length != 32 || !value.All(Uri.IsHexDigit))
            throw new BusinessRuleViolationException("Scheduled broadcast ID is invalid.");
        return value;
    }

    private static NotificationTemplateResponse? TryMapTemplate(SystemSetting setting)
    {
        try
        {
            var value = JsonSerializer.Deserialize<NotificationTemplateResponse>(setting.Value, JsonOptions);
            if (value is null) return null;
            value.UpdatedAt = setting.UpdatedAt;
            return value;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static NotificationDeliveryResponse? TryMapDelivery(AuditLog audit)
    {
        var payload = DeserializeDelivery(audit.NewValuesJson);
        if (payload is null) return null;
        return new NotificationDeliveryResponse
        {
            Id = audit.Id,
            BroadcastId = payload.BroadcastId,
            RecipientUserId = payload.RecipientUserId,
            RecipientName = payload.RecipientName,
            RecipientEmail = payload.RecipientEmail,
            Channel = payload.Channel,
            Status = payload.Status,
            Provider = payload.Provider,
            ProviderMessageId = payload.ProviderMessageId,
            Title = payload.Title,
            Message = payload.Message,
            Type = payload.Type,
            ActionUrl = payload.ActionUrl,
            TemplateKey = payload.TemplateKey,
            AttemptNumber = payload.AttemptNumber,
            RetriedFromLogId = payload.RetriedFromLogId,
            Error = payload.Error,
            CreatedAt = audit.CreatedAt
        };
    }

    private static DeliveryRecord? DeserializeDelivery(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<DeliveryRecord>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static string? MergeRetryReason(string? json, string reason)
    {
        var payload = DeserializeDelivery(json);
        if (payload is null) return json;
        payload.RetryReason = reason;
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static ScheduledBroadcastState? DeserializeSchedule(string json)
    {
        try { return JsonSerializer.Deserialize<ScheduledBroadcastState>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static ScheduledBroadcastResponse? TryMapSchedule(SystemSetting setting)
    {
        var state = DeserializeSchedule(setting.Value);
        if (state is null) return null;
        var audienceType = string.IsNullOrWhiteSpace(state.AudienceType)
            ? CanonicalAudienceType(state.Request)
            : state.AudienceType;
        return new ScheduledBroadcastResponse
        {
            Id = state.Id,
            Status = state.Status,
            TemplateKey = state.Request.TemplateKey,
            Title = state.Request.Title,
            AudienceType = audienceType,
            AudienceLabel = string.IsNullOrWhiteSpace(state.AudienceLabel) ? DescribeAudienceRequest(state.Request) : state.AudienceLabel,
            Role = state.Request.Role,
            DepartmentId = state.Request.DepartmentId,
            WardId = state.Request.WardId,
            GroupIdentifier = state.Request.GroupIdentifier,
            SelectedUserCount = state.Request.SelectedUserIds?.Count ?? 0,
            ScheduledFor = state.ScheduledFor,
            CreatedAt = state.CreatedAt,
            CompletedAt = state.CompletedAt,
            RecipientCount = state.RecipientCount,
            Error = state.Error
        };
    }


    private async Task<List<NotificationDeliveryResponse>> LoadFilteredDeliveryLogsAsync(NotificationDeliveryQuery query, CancellationToken cancellationToken)
    {
        var audits = await _db.AuditLogs.AsNoTracking()
            .Where(entity => entity.Action == DeliveryAction)
            .OrderByDescending(entity => entity.CreatedAt)
            .Take(20000)
            .ToListAsync(cancellationToken);
        IEnumerable<NotificationDeliveryResponse> items = audits.Select(TryMapDelivery).Where(item => item is not null).Cast<NotificationDeliveryResponse>();

        if (!string.IsNullOrWhiteSpace(query.Channel))
            items = items.Where(item => item.Channel.Equals(query.Channel.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(item => item.Status.Equals(query.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.BroadcastId))
            items = items.Where(item => item.BroadcastId.Equals(query.BroadcastId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(item =>
                item.RecipientName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.RecipientEmail.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return items.ToList();
    }

    private long RequireAdminId()
    {
        if (_currentUser.Role is not (nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Admin permission is required.");
        return _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
    }

    private static string? Truncate(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ResolvedBroadcastContent(string Title, string Message, string Type, string? ActionUrl, string? TemplateKey);
    private sealed record ResolvedAudience(string Type, string Label, IReadOnlyList<User> Users);

    private sealed class DeliveryRecord
    {
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
        public int AttemptNumber { get; set; } = 1;
        public long? RetriedFromLogId { get; set; }
        public string? RetryReason { get; set; }
        public string? Error { get; set; }
    }

    private sealed class ScheduledBroadcastState
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = ScheduleStatuses.Pending;
        public AdminBroadcastNotificationRequest Request { get; set; } = new();
        public string AudienceType { get; set; } = string.Empty;
        public string AudienceLabel { get; set; } = string.Empty;
        public DateTimeOffset ScheduledFor { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastAttemptAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public long CreatedByUserId { get; set; }
        public string? CreatedByEmail { get; set; }
        public int RecipientCount { get; set; }
        public string? Error { get; set; }
    }

    private static class DeliveryStatuses
    {
        public const string Sent = "Sent";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
        public const string NotConfigured = "NotConfigured";
    }

    private static class ScheduleStatuses
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
    }
}
