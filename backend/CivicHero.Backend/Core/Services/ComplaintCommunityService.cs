using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class ComplaintCommunityService : IComplaintCommunityService
{
    private const string FollowEntityName = "ComplaintFollow";
    private const string FollowedAction = "ComplaintFollowed";
    private const string UnfollowedAction = "ComplaintUnfollowed";

    private readonly IComplaintRepository _complaints;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ComplaintCommunityService(
        IComplaintRepository complaints,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        INotificationService notifications)
    {
        _complaints = complaints;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ComplaintFollowStatusResponse> FollowAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var complaint = await RequireFollowableComplaintAsync(complaintId, cancellationToken);
        var userId = RequireUserId();
        var latest = await GetLatestUserFollowLogAsync(userId, complaintId, cancellationToken);
        if (latest?.Action != FollowedAction)
        {
            await _unitOfWork.Repository<AuditLog>().AddAsync(CreateFollowAudit(
                userId,
                complaintId,
                FollowedAction,
                new { complaint.Title, complaint.Status }), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await BuildStatusAsync(complaintId, userId, cancellationToken);
    }

    public async Task<ComplaintFollowStatusResponse> UnfollowAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        await RequireComplaintAsync(complaintId, cancellationToken);
        var userId = RequireUserId();
        var latest = await GetLatestUserFollowLogAsync(userId, complaintId, cancellationToken);
        if (latest?.Action == FollowedAction)
        {
            await _unitOfWork.Repository<AuditLog>().AddAsync(CreateFollowAudit(
                userId,
                complaintId,
                UnfollowedAction,
                new { reason = "Citizen removed the complaint from the following feed." }), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await BuildStatusAsync(complaintId, userId, cancellationToken);
    }

    public async Task<ComplaintFollowStatusResponse> GetFollowStatusAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        await RequireComplaintAsync(complaintId, cancellationToken);
        return await BuildStatusAsync(complaintId, RequireUserId(), cancellationToken);
    }

    public async Task<ComplaintFollowingIdsResponse> GetFollowingIdsAsync(CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var active = await GetActiveFollowingForUserAsync(RequireUserId(), cancellationToken);
        return new ComplaintFollowingIdsResponse { ComplaintIds = active.Keys.OrderBy(id => id).ToArray() };
    }

    public async Task<PagedResponse<FollowedComplaintResponse>> GetFollowingAsync(ComplaintQuery query, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var active = await GetActiveFollowingForUserAsync(RequireUserId(), cancellationToken);
        if (active.Count == 0)
        {
            return new PagedResponse<FollowedComplaintResponse>
            {
                Items = [],
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = 0
            };
        }

        var ids = active.Keys.ToArray();
        var source = _complaints.QueryWithSummary()
            .Where(entity => ids.Contains(entity.Id) &&
                             entity.Status != ComplaintStatus.Withdrawn &&
                             entity.Status != ComplaintStatus.ClosedFraud &&
                             entity.Status != ComplaintStatus.Merged);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ComplaintStatus>(query.Status, true, out var status))
            source = source.Where(entity => entity.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Category))
            source = source.Where(entity => entity.Category == query.Category.Trim());
        if (query.DepartmentId.HasValue)
            source = source.Where(entity => entity.DepartmentId == query.DepartmentId.Value);
        if (query.WardId.HasValue)
            source = source.Where(entity => entity.WardId == query.WardId.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            source = source.Where(entity =>
                EF.Functions.Like(entity.Title, search) ||
                EF.Functions.Like(entity.Description, search) ||
                EF.Functions.Like(entity.Address, search));
        }

        source = query.SortBy.Trim().ToLowerInvariant() switch
        {
            "oldest" => source.OrderBy(entity => entity.CreatedAt),
            "most-supported" => source.OrderByDescending(entity => entity.Votes.Count).ThenByDescending(entity => entity.UpdatedAt),
            "recently-updated" => source.OrderByDescending(entity => entity.UpdatedAt),
            "followed" => source.OrderByDescending(entity => entity.Id),
            _ => source.OrderByDescending(entity => entity.UpdatedAt)
        };

        var total = await source.CountAsync(cancellationToken);
        var complaints = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var followerCounts = await GetFollowerCountsAsync(complaints.Select(entity => entity.Id), cancellationToken);

        var items = complaints.Select(entity => new FollowedComplaintResponse
        {
            Id = entity.Id,
            ReferenceNumber = $"CH-{entity.CreatedAt:yyyy}-{entity.Id:D6}",
            Title = entity.Title,
            Description = entity.Description,
            Category = entity.Category,
            Status = entity.Status.ToString(),
            Priority = entity.Priority.ToString(),
            DepartmentName = entity.Department.Name,
            WardName = entity.Ward.Name,
            Address = entity.Address,
            UpvoteCount = entity.Votes.Count,
            ImageCount = entity.Images.Count,
            FollowerCount = followerCounts.GetValueOrDefault(entity.Id),
            FollowedAt = active[entity.Id].CreatedAt,
            LastActivityAt = entity.UpdatedAt,
            CreatedAt = entity.CreatedAt
        }).ToArray();

        if (query.SortBy.Equals("followed", StringComparison.OrdinalIgnoreCase))
            items = items.OrderByDescending(item => item.FollowedAt).ToArray();

        return new PagedResponse<FollowedComplaintResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<int> NotifyFollowersAsync(
        long complaintId,
        string title,
        string message,
        long? actorUserId = null,
        IReadOnlyCollection<long>? excludedUserIds = null,
        CancellationToken cancellationToken = default)
    {
        var activeFollowers = await GetActiveFollowersAsync(complaintId, cancellationToken);
        if (activeFollowers.Count == 0) return 0;

        var excluded = excludedUserIds is null
            ? new HashSet<long>()
            : new HashSet<long>(excludedUserIds);
        if (actorUserId.HasValue) excluded.Add(actorUserId.Value);

        var sent = 0;
        foreach (var userId in activeFollowers.Keys.Where(id => !excluded.Contains(id)))
        {
            var result = await _notifications.SendAsync(new NotificationDispatchRequest(
                userId,
                title,
                message,
                nameof(NotificationType.ComplaintProgress),
                "Complaint",
                complaintId,
                $"/citizen/complaints/{complaintId}"), cancellationToken);
            if (result is not null) sent++;
        }

        return sent;
    }

    private async Task<ComplaintFollowStatusResponse> BuildStatusAsync(long complaintId, long userId, CancellationToken cancellationToken)
    {
        var latest = await GetLatestUserFollowLogAsync(userId, complaintId, cancellationToken);
        var followers = await GetActiveFollowersAsync(complaintId, cancellationToken);
        return new ComplaintFollowStatusResponse
        {
            ComplaintId = complaintId,
            IsFollowing = latest?.Action == FollowedAction,
            FollowedAt = latest?.Action == FollowedAction ? latest.CreatedAt : null,
            FollowerCount = followers.Count
        };
    }

    private async Task<Dictionary<long, AuditLog>> GetActiveFollowingForUserAsync(long userId, CancellationToken cancellationToken)
    {
        var logs = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.UserId == userId &&
                          log.EntityName == FollowEntityName &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .OrderByDescending(log => log.CreatedAt)
            .Take(5000)
            .ToListAsync(cancellationToken);

        return logs
            .Where(log => long.TryParse(log.EntityId, out _))
            .GroupBy(log => long.Parse(log.EntityId!))
            .Select(group => group.First())
            .Where(log => log.Action == FollowedAction)
            .ToDictionary(log => long.Parse(log.EntityId!), log => log);
    }

    private async Task<Dictionary<long, AuditLog>> GetActiveFollowersAsync(long complaintId, CancellationToken cancellationToken)
    {
        var entityId = complaintId.ToString();
        var logs = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.UserId.HasValue &&
                          log.EntityName == FollowEntityName &&
                          log.EntityId == entityId &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .OrderByDescending(log => log.CreatedAt)
            .Take(20000)
            .ToListAsync(cancellationToken);

        return logs
            .GroupBy(log => log.UserId!.Value)
            .Select(group => group.First())
            .Where(log => log.Action == FollowedAction)
            .ToDictionary(log => log.UserId!.Value, log => log);
    }

    private async Task<Dictionary<long, int>> GetFollowerCountsAsync(IEnumerable<long> complaintIds, CancellationToken cancellationToken)
    {
        var ids = complaintIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        var entityIds = ids.Select(id => id.ToString()).ToArray();
        var logs = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.UserId.HasValue &&
                          log.EntityName == FollowEntityName &&
                          log.EntityId != null && entityIds.Contains(log.EntityId) &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .OrderByDescending(log => log.CreatedAt)
            .Take(50000)
            .ToListAsync(cancellationToken);

        return logs
            .Where(log => long.TryParse(log.EntityId, out _))
            .GroupBy(log => new { ComplaintId = long.Parse(log.EntityId!), UserId = log.UserId!.Value })
            .Select(group => group.First())
            .Where(log => log.Action == FollowedAction)
            .GroupBy(log => long.Parse(log.EntityId!))
            .ToDictionary(group => group.Key, group => group.Count());
    }

    private Task<AuditLog?> GetLatestUserFollowLogAsync(long userId, long complaintId, CancellationToken cancellationToken) =>
        _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.UserId == userId &&
                          log.EntityName == FollowEntityName &&
                          log.EntityId == complaintId.ToString() &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .OrderByDescending(log => log.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<Complaint> RequireFollowableComplaintAsync(long complaintId, CancellationToken cancellationToken)
    {
        var complaint = await RequireComplaintAsync(complaintId, cancellationToken);
        if (complaint.Status is ComplaintStatus.Withdrawn or ComplaintStatus.ClosedFraud or ComplaintStatus.Merged)
            throw new BusinessRuleViolationException("This complaint is not available for following.");
        return complaint;
    }

    private async Task<Complaint> RequireComplaintAsync(long complaintId, CancellationToken cancellationToken) =>
        await _complaints.QueryWithSummary()
            .FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
        ?? throw new NotFoundException("Complaint was not found.");

    private AuditLog CreateFollowAudit(long userId, long complaintId, string action, object values) => new()
    {
        UserId = userId,
        UserEmail = _currentUser.Email,
        UserRole = _currentUser.Role,
        Action = action,
        EntityName = FollowEntityName,
        EntityId = complaintId.ToString(),
        NewValuesJson = System.Text.Json.JsonSerializer.Serialize(values),
        Severity = "Information",
        Success = true,
        HttpStatusCode = 200,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private long RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");

    private void EnsureCitizen()
    {
        if (_currentUser.Role != nameof(UserRole.Citizen))
            throw new UnauthorizedAccessException("Citizen permission is required.");
    }
}
