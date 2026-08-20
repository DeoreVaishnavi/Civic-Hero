using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IHubContext<NotificationHub> hub)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _hub = hub;
    }

    public async Task<NotificationListResponse> GetAsync(NotificationQuery query, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = _unitOfWork.Repository<Notification>().Query()
            .Where(entity => entity.UserId == userId && !entity.IsArchived && (!entity.ExpiresAt.HasValue || entity.ExpiresAt > DateTimeOffset.UtcNow));

        if (query.UnreadOnly) source = source.Where(entity => !entity.IsRead);
        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<NotificationType>(query.Type, true, out var type))
            source = source.Where(entity => entity.Type == type);

        var totalCount = await source.CountAsync(cancellationToken);
        var unreadCount = await _unitOfWork.Repository<Notification>().Query()
            .CountAsync(entity => entity.UserId == userId && !entity.IsArchived && !entity.IsRead, cancellationToken);
        var entities = await source.OrderByDescending(entity => entity.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(Map).ToList();

        return new NotificationListResponse { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount, UnreadCount = unreadCount };
    }

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        return _unitOfWork.Repository<Notification>().Query()
            .CountAsync(entity => entity.UserId == userId && !entity.IsArchived && !entity.IsRead, cancellationToken);
    }

    public async Task<NotificationResponse> MarkReadAsync(long id, CancellationToken cancellationToken = default)
    {
        var notification = await FindOwnedAsync(id, true, cancellationToken);
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Map(notification);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var now = DateTimeOffset.UtcNow;
        var unread = await _unitOfWork.Repository<Notification>().Query(true)
            .Where(entity => entity.UserId == userId && !entity.IsArchived && !entity.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }
        if (unread.Count > 0) await _unitOfWork.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var notification = await FindOwnedAsync(id, true, cancellationToken);
        notification.IsArchived = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationPreferencesResponse> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(RequireUserId(), cancellationToken);
        return Map(preference);
    }

    public async Task<NotificationPreferencesResponse> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(RequireUserId(), cancellationToken);
        preference.InAppEnabled = request.InAppEnabled;
        preference.EmailEnabled = request.EmailEnabled;
        preference.SmsEnabled = request.SmsEnabled;
        preference.ComplaintUpdates = request.ComplaintUpdates;
        preference.AssignmentUpdates = request.AssignmentUpdates;
        preference.VerificationUpdates = request.VerificationUpdates;
        preference.DisputeUpdates = request.DisputeUpdates;
        preference.RewardUpdates = request.RewardUpdates;
        preference.SecurityAlerts = request.SecurityAlerts;
        preference.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(preference);
    }

    public async Task<NotificationResponse?> SendAsync(NotificationDispatchRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationType>(request.Type, true, out var type)) type = NotificationType.General;
        var preference = await GetOrCreatePreferenceAsync(request.UserId, cancellationToken);
        if (!preference.InAppEnabled || !IsCategoryEnabled(preference, type)) return null;

        var entity = new Notification
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Type = type,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            ActionUrl = request.ActionUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _unitOfWork.Repository<Notification>().AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var response = Map(entity);
        if (request.SendSignalR)
            await _hub.Clients.Group($"user_{request.UserId}").SendAsync("notificationReceived", response, cancellationToken);
        return response;
    }

    public async Task<int> BroadcastAsync(BroadcastNotificationRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var users = _unitOfWork.Repository<User>().Query().Where(entity => entity.IsActive && entity.IsEmailVerified && !entity.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var role))
            users = users.Where(entity => entity.Role == role);
        var userIds = await users.Select(entity => entity.Id).ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var userId in userIds)
        {
            var created = await SendAsync(new NotificationDispatchRequest(userId, request.Title, request.Message, nameof(NotificationType.General), "Broadcast", null, request.ActionUrl), cancellationToken);
            if (created is not null) sent++;
        }
        return sent;
    }

    private async Task<Notification> FindOwnedAsync(long id, bool tracking, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        return await _unitOfWork.Repository<Notification>().Query(tracking)
            .FirstOrDefaultAsync(entity => entity.Id == id && entity.UserId == userId && !entity.IsArchived, cancellationToken)
            ?? throw new NotFoundException("Notification was not found.");
    }

    private async Task<NotificationPreference> GetOrCreatePreferenceAsync(long userId, CancellationToken cancellationToken)
    {
        var preference = await _unitOfWork.Repository<NotificationPreference>().Query(true)
            .FirstOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);
        if (preference is not null) return preference;
        preference = new NotificationPreference { UserId = userId, UpdatedAt = DateTimeOffset.UtcNow };
        await _unitOfWork.Repository<NotificationPreference>().AddAsync(preference, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return preference;
    }

    private static bool IsCategoryEnabled(NotificationPreference preference, NotificationType type) => type switch
    {
        NotificationType.ComplaintCreated or NotificationType.ComplaintProgress => preference.ComplaintUpdates,
        NotificationType.ComplaintAssigned or NotificationType.SlaEscalation => preference.AssignmentUpdates,
        NotificationType.ResolutionReady or NotificationType.VerificationRequired => preference.VerificationUpdates,
        NotificationType.DisputeUpdate => preference.DisputeUpdates,
        NotificationType.RewardEarned => preference.RewardUpdates,
        NotificationType.SecurityAlert => preference.SecurityAlerts,
        _ => true
    };

    private long RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
    private void EnsureAdmin()
    {
        if (_currentUser.Role is not (nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Admin permission is required.");
    }

    private static NotificationResponse Map(Notification entity) => new()
    {
        Id = entity.Id, Title = entity.Title, Message = entity.Message, Type = entity.Type.ToString(),
        ReferenceType = entity.ReferenceType, ReferenceId = entity.ReferenceId, ActionUrl = entity.ActionUrl,
        IsRead = entity.IsRead, CreatedAt = entity.CreatedAt, ReadAt = entity.ReadAt
    };

    private static NotificationPreferencesResponse Map(NotificationPreference entity) => new()
    {
        InAppEnabled = entity.InAppEnabled, EmailEnabled = entity.EmailEnabled, SmsEnabled = entity.SmsEnabled,
        ComplaintUpdates = entity.ComplaintUpdates, AssignmentUpdates = entity.AssignmentUpdates,
        VerificationUpdates = entity.VerificationUpdates, DisputeUpdates = entity.DisputeUpdates,
        RewardUpdates = entity.RewardUpdates, SecurityAlerts = entity.SecurityAlerts
    };
}
