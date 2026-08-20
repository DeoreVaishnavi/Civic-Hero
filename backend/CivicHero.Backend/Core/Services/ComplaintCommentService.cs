using CivicHero.Backend.Core.DTOs.Comments;
using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class ComplaintCommentService : IComplaintCommentService
{
    private const string ReportEntityName = "ComplaintCommentReport";
    private const string ReportAction = "CommentReported";
    private const int AutomaticHideThreshold = 3;
    private static readonly string[] AutoHidePatterns = ["<script", "javascript:", "buy now", "free money", "http://", "https://"];
    private static readonly HashSet<string> ReportCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unsafe", "Abusive", "Harassment", "Spam", "PersonalInformation", "Misinformation", "Other"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IComplaintCommunityService _community;

    public ComplaintCommentService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IComplaintCommunityService community)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notifications = notifications;
        _community = community;
    }

    public async Task<IReadOnlyList<ComplaintCommentResponse>> GetAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        var complaint = await LoadComplaintAsync(complaintId, cancellationToken);
        EnsureCanView(complaint);
        var isStaff = IsStaff();
        var comments = complaint.Comments
            .Where(comment => isStaff || (comment.Visibility == CommentVisibility.Public && comment.ModerationStatus == CommentModerationStatus.Visible))
            .OrderBy(comment => comment.CreatedAt)
            .ToArray();

        if (comments.Length == 0) return [];
        var commentIds = comments.Select(comment => comment.Id.ToString()).ToArray();
        var reports = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.EntityName == ReportEntityName &&
                          log.Action == ReportAction &&
                          log.EntityId != null && commentIds.Contains(log.EntityId))
            .ToListAsync(cancellationToken);
        var counts = reports
            .Where(log => long.TryParse(log.EntityId, out _))
            .GroupBy(log => long.Parse(log.EntityId!))
            .ToDictionary(group => group.Key, group => group.Select(log => log.UserId).Distinct().Count());
        var userId = _currentUser.UserId;
        HashSet<long> reportedByCurrent = userId.HasValue
            ? reports.Where(log => log.UserId == userId.Value && long.TryParse(log.EntityId, out _))
                .Select(log => long.Parse(log.EntityId!)).ToHashSet()
            : new HashSet<long>();

        return comments.Select(comment => Map(
            comment,
            counts.GetValueOrDefault(comment.Id),
            reportedByCurrent.Contains(comment.Id))).ToArray();
    }

    public async Task<PagedResponse<ComplaintCommentModerationItemResponse>> GetModerationQueueAsync(ComplaintCommentModerationQuery query, CancellationToken cancellationToken = default)
    {
        EnsureModerator();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = _unitOfWork.Repository<ComplaintComment>().Query()
            .Include(entity => entity.User)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .AsQueryable();
        if (!IsAdmin())
        {
            var departmentId = _currentUser.DepartmentId ?? -1;
            source = source.Where(entity => entity.Complaint.DepartmentId == departmentId);
        }
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<CommentModerationStatus>(query.Status, true, out var status))
            source = source.Where(entity => entity.ModerationStatus == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            source = source.Where(entity => EF.Functions.Like(entity.Body, search) || EF.Functions.Like(entity.Complaint.Title, search));
        }
        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(entity => entity.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponse<ComplaintCommentModerationItemResponse>
        {
            Items = items.Select(entity => new ComplaintCommentModerationItemResponse
            {
                Id = entity.Id,
                ComplaintId = entity.ComplaintId,
                ReferenceNumber = $"CH-{entity.Complaint.CreatedAt:yyyy}-{entity.ComplaintId:D6}",
                ComplaintTitle = entity.Complaint.Title,
                DepartmentName = entity.Complaint.Department.Name,
                WardName = entity.Complaint.Ward.Name,
                AuthorName = entity.User.FullName,
                AuthorRole = entity.User.Role.ToString(),
                Body = entity.Body,
                Visibility = entity.Visibility.ToString(),
                ModerationStatus = entity.ModerationStatus.ToString(),
                ModerationReason = entity.ModerationReason,
                CreatedAt = entity.CreatedAt
            }).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ComplaintCommentResponse> AddAsync(long complaintId, AddComplaintCommentRequest request, CancellationToken cancellationToken = default)
    {
        var complaint = await LoadComplaintAsync(complaintId, cancellationToken, tracking: true);
        EnsureCanView(complaint);
        var body = ValidateBody(request.Body);
        var visibility = Enum.TryParse<CommentVisibility>(request.Visibility, true, out var parsed) ? parsed : CommentVisibility.Public;
        if (!IsStaff() && visibility != CommentVisibility.Public)
            throw new UnauthorizedAccessException("Citizens can only add public comments.");
        if (complaint.IsAnonymous && !IsStaff())
            throw new BusinessRuleViolationException("Anonymous complaints do not support citizen comments.");

        var hidden = ContainsUnsafePattern(body);
        var comment = new ComplaintComment
        {
            ComplaintId = complaint.Id,
            UserId = RequireUserId(),
            Body = body,
            Visibility = visibility,
            ModerationStatus = hidden ? CommentModerationStatus.Hidden : CommentModerationStatus.Visible,
            ModerationReason = hidden ? "Automatically hidden for moderator review." : null
        };
        await _unitOfWork.Repository<ComplaintComment>().AddAsync(comment, cancellationToken);
        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = RequireUserId(),
            EventType = hidden ? "COMMENT_HELD_FOR_MODERATION" : "COMMENT_ADDED",
            Description = visibility == CommentVisibility.Internal ? "An internal operational note was added." : "A public comment was added.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        comment.User = await _unitOfWork.Repository<User>().GetByIdAsync(comment.UserId, cancellationToken)
            ?? throw new NotFoundException("Comment author was not found.");

        if (!hidden && visibility == CommentVisibility.Public && !complaint.IsAnonymous)
        {
            if (comment.UserId != complaint.CitizenId)
                await NotifyAsync(complaint.CitizenId, complaint, "New complaint comment", "A new comment was added to your complaint.", cancellationToken);
            if (complaint.AssignedOfficerId.HasValue && comment.UserId != complaint.AssignedOfficerId.Value)
                await NotifyAsync(complaint.AssignedOfficerId.Value, complaint, "New complaint comment", "A new comment was added to an assigned complaint.", cancellationToken);

            var excluded = new List<long> { complaint.CitizenId };
            if (complaint.AssignedOfficerId.HasValue) excluded.Add(complaint.AssignedOfficerId.Value);
            await _community.NotifyFollowersAsync(
                complaint.Id,
                "New update on a followed complaint",
                $"A public comment was added to {complaint.Title}.",
                comment.UserId,
                excluded,
                cancellationToken);
        }
        return Map(comment, 0, false);
    }

    public async Task<ComplaintCommentResponse> UpdateAsync(long complaintId, long commentId, UpdateComplaintCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Repository<ComplaintComment>().Query(true)
            .Include(entity => entity.User)
            .Include(entity => entity.Complaint)
            .FirstOrDefaultAsync(entity => entity.Id == commentId && entity.ComplaintId == complaintId, cancellationToken)
            ?? throw new NotFoundException("Comment was not found.");
        EnsureCanView(comment.Complaint);
        if (comment.UserId != RequireUserId())
            throw new UnauthorizedAccessException("Only the comment author can edit this comment.");
        if (comment.Visibility != CommentVisibility.Public)
            throw new BusinessRuleViolationException("Only public comments can be edited through the community discussion.");
        if (comment.ModerationStatus != CommentModerationStatus.Visible)
            throw new BusinessRuleViolationException("A hidden or removed comment cannot be edited until moderation is complete.");

        var body = ValidateBody(request.Body);
        var hidden = ContainsUnsafePattern(body);
        comment.Body = body;
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        if (hidden)
        {
            comment.ModerationStatus = CommentModerationStatus.Hidden;
            comment.ModerationReason = "Edited content was automatically hidden for moderator review.";
        }
        comment.Complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = RequireUserId(),
            EventType = hidden ? "COMMENT_EDIT_HELD_FOR_MODERATION" : "COMMENT_EDITED",
            Description = hidden ? "An edited public comment was held for moderation." : "A public comment was edited.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!hidden)
        {
            await _community.NotifyFollowersAsync(
                complaintId,
                "Comment updated on a followed complaint",
                $"A public comment was updated on {comment.Complaint.Title}.",
                comment.UserId,
                [comment.Complaint.CitizenId],
                cancellationToken);
        }

        var reportState = await GetReportStateAsync(commentId, cancellationToken);
        return Map(comment, reportState.Count, reportState.ReportedByCurrentUser);
    }

    public async Task<ComplaintCommentReportResponse> ReportAsync(long complaintId, long commentId, ReportComplaintCommentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCitizen();
        var comment = await _unitOfWork.Repository<ComplaintComment>().Query(true)
            .Include(entity => entity.User)
            .Include(entity => entity.Complaint)
            .FirstOrDefaultAsync(entity => entity.Id == commentId && entity.ComplaintId == complaintId, cancellationToken)
            ?? throw new NotFoundException("Comment was not found.");
        EnsureCanView(comment.Complaint);
        var userId = RequireUserId();
        if (comment.UserId == userId)
            throw new BusinessRuleViolationException("You cannot report your own comment.");
        if (comment.Visibility != CommentVisibility.Public || comment.ModerationStatus == CommentModerationStatus.Removed)
            throw new BusinessRuleViolationException("Only an available public comment can be reported.");

        var category = (request.Category ?? string.Empty).Trim();
        if (!ReportCategories.Contains(category))
            throw new ValidationException(["Report category must be Unsafe, Abusive, Harassment, Spam, PersonalInformation, Misinformation or Other."]);
        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length is < 5 or > 500)
            throw new ValidationException(["Report reason must contain 5 to 500 characters."]);

        var entityId = commentId.ToString();
        var alreadyReported = await _unitOfWork.Repository<AuditLog>().Query()
            .AnyAsync(log => log.UserId == userId && log.EntityName == ReportEntityName && log.EntityId == entityId && log.Action == ReportAction, cancellationToken);
        if (alreadyReported)
            throw new BusinessRuleViolationException("You have already reported this comment.");

        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = userId,
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = ReportAction,
            EntityName = ReportEntityName,
            EntityId = entityId,
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { complaintId, category, reason }),
            Severity = "Warning",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reportCount = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.EntityName == ReportEntityName && log.EntityId == entityId && log.Action == ReportAction && log.UserId.HasValue)
            .Select(log => log.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var automaticallyHidden = reportCount >= AutomaticHideThreshold && comment.ModerationStatus == CommentModerationStatus.Visible;
        if (automaticallyHidden)
        {
            comment.ModerationStatus = CommentModerationStatus.Hidden;
            comment.ModerationReason = $"Automatically hidden after {reportCount} independent citizen reports.";
            comment.ModeratedAt = DateTimeOffset.UtcNow;
            comment.Complaint.Timeline.Add(new ComplaintTimeline
            {
                UserId = userId,
                EventType = "COMMENT_REPORTED_AND_HIDDEN",
                Description = "A public comment was hidden pending moderator review after multiple safety reports.",
                Timestamp = DateTimeOffset.UtcNow
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await NotifyModeratorsAsync(comment, category, reason, reportCount, cancellationToken);
        if (automaticallyHidden)
        {
            await _notifications.SendAsync(new NotificationDispatchRequest(
                comment.UserId,
                "Comment hidden for safety review",
                "One of your public comments was temporarily hidden after multiple reports and is awaiting moderator review.",
                nameof(NotificationType.General),
                "ComplaintComment",
                comment.Id,
                $"/citizen/complaints/{complaintId}"), cancellationToken);
        }

        return new ComplaintCommentReportResponse
        {
            CommentId = commentId,
            ReportCount = reportCount,
            ReportedByCurrentUser = true,
            ModerationStatus = comment.ModerationStatus.ToString(),
            AutomaticallyHidden = automaticallyHidden
        };
    }

    public async Task DeleteAsync(long complaintId, long commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Repository<ComplaintComment>().Query(true)
            .Include(entity => entity.Complaint)
            .FirstOrDefaultAsync(entity => entity.Id == commentId && entity.ComplaintId == complaintId, cancellationToken)
            ?? throw new NotFoundException("Comment was not found.");
        EnsureCanView(comment.Complaint);
        if (comment.UserId != RequireUserId() && !IsAdmin())
            throw new UnauthorizedAccessException("Only the author or an administrator can delete this comment.");
        comment.Complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = RequireUserId(),
            EventType = "COMMENT_DELETED",
            Description = "A comment was deleted by its author or an administrator.",
            Timestamp = DateTimeOffset.UtcNow
        });
        _unitOfWork.Repository<ComplaintComment>().Remove(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ComplaintCommentResponse> ModerateAsync(long complaintId, long commentId, ModerateComplaintCommentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureModerator();
        var comment = await _unitOfWork.Repository<ComplaintComment>().Query(true)
            .Include(entity => entity.User)
            .Include(entity => entity.Complaint)
            .FirstOrDefaultAsync(entity => entity.Id == commentId && entity.ComplaintId == complaintId, cancellationToken)
            ?? throw new NotFoundException("Comment was not found.");
        EnsureScope(comment.Complaint);
        if (!Enum.TryParse<CommentModerationStatus>(request.Decision, true, out var decision))
            throw new ValidationException(["Moderation decision must be Visible, Hidden or Removed."]);
        comment.ModerationStatus = decision;
        comment.ModerationReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        comment.ModeratedByUserId = RequireUserId();
        comment.ModeratedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var reportState = await GetReportStateAsync(commentId, cancellationToken);
        return Map(comment, reportState.Count, reportState.ReportedByCurrentUser);
    }

    private async Task<(int Count, bool ReportedByCurrentUser)> GetReportStateAsync(long commentId, CancellationToken cancellationToken)
    {
        var entityId = commentId.ToString();
        var reports = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.EntityName == ReportEntityName && log.EntityId == entityId && log.Action == ReportAction)
            .ToListAsync(cancellationToken);
        var userId = _currentUser.UserId;
        return (reports.Select(log => log.UserId).Distinct().Count(), userId.HasValue && reports.Any(log => log.UserId == userId.Value));
    }

    private async Task NotifyModeratorsAsync(ComplaintComment comment, string category, string reason, int reportCount, CancellationToken cancellationToken)
    {
        var moderatorIds = await _unitOfWork.Repository<User>().Query()
            .Where(user => user.IsActive && !user.IsDeleted &&
                (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin ||
                 (user.Role == UserRole.Supervisor && user.DepartmentId == comment.Complaint.DepartmentId &&
                  (!user.WardId.HasValue || user.WardId == comment.Complaint.WardId))))
            .Select(user => user.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var moderatorId in moderatorIds)
        {
            await _notifications.SendAsync(new NotificationDispatchRequest(
                moderatorId,
                "Public comment safety report",
                $"{reportCount} report(s) for a {category} comment on {comment.Complaint.Title}: {reason}",
                nameof(NotificationType.General),
                "ComplaintComment",
                comment.Id,
                "/supervisor/comment-moderation"), cancellationToken);
        }
    }

    private async Task<Complaint> LoadComplaintAsync(long id, CancellationToken cancellationToken, bool tracking = false) =>
        await _unitOfWork.Repository<Complaint>().Query(tracking)
            .Include(entity => entity.Comments).ThenInclude(entity => entity.User)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
        ?? throw new NotFoundException("Complaint was not found.");

    private static string ValidateBody(string? value)
    {
        var body = (value ?? string.Empty).Trim();
        if (body.Length is < 2 or > 1500)
            throw new ValidationException(["Comment must contain 2 to 1500 characters."]);
        return body;
    }

    private static bool ContainsUnsafePattern(string body) =>
        AutoHidePatterns.Any(pattern => body.Contains(pattern, StringComparison.OrdinalIgnoreCase));

    private void EnsureCanView(Complaint complaint)
    {
        if (IsAdmin()) return;
        if (_currentUser.Role == nameof(UserRole.Supervisor) && complaint.DepartmentId == _currentUser.DepartmentId) return;
        if (_currentUser.Role == nameof(UserRole.Officer) && complaint.DepartmentId == _currentUser.DepartmentId && complaint.WardId == _currentUser.WardId) return;
        if (_currentUser.Role == nameof(UserRole.Citizen) && (complaint.CitizenId == RequireUserId() || complaint.Status != ComplaintStatus.Withdrawn)) return;
        throw new UnauthorizedAccessException("This complaint is outside your access scope.");
    }

    private void EnsureScope(Complaint complaint)
    {
        if (IsAdmin()) return;
        if (_currentUser.Role == nameof(UserRole.Supervisor) && complaint.DepartmentId == _currentUser.DepartmentId) return;
        throw new UnauthorizedAccessException("This complaint is outside your moderation scope.");
    }

    private void EnsureModerator()
    {
        if (_currentUser.Role is not (nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Moderator permission is required.");
    }

    private void EnsureCitizen()
    {
        if (_currentUser.Role != nameof(UserRole.Citizen))
            throw new UnauthorizedAccessException("Citizen permission is required.");
    }

    private bool IsStaff() => _currentUser.Role is nameof(UserRole.Officer) or nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);
    private bool IsAdmin() => _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);
    private long RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");

    private async Task NotifyAsync(long userId, Complaint complaint, string title, string message, CancellationToken cancellationToken) =>
        await _notifications.SendAsync(new NotificationDispatchRequest(
            userId,
            title,
            message,
            nameof(NotificationType.ComplaintProgress),
            "Complaint",
            complaint.Id,
            $"/citizen/complaints/{complaint.Id}"), cancellationToken);

    private ComplaintCommentResponse Map(ComplaintComment entity, int reportCount, bool reportedByCurrentUser)
    {
        var userId = _currentUser.UserId;
        var isCitizen = _currentUser.Role == nameof(UserRole.Citizen);
        var isAuthor = userId.HasValue && entity.UserId == userId.Value;
        var isAvailablePublic = entity.Visibility == CommentVisibility.Public && entity.ModerationStatus == CommentModerationStatus.Visible;
        return new ComplaintCommentResponse
        {
            Id = entity.Id,
            ComplaintId = entity.ComplaintId,
            UserId = entity.UserId,
            AuthorName = entity.User.FullName,
            AuthorRole = entity.User.Role.ToString(),
            Body = entity.Body,
            Visibility = entity.Visibility.ToString(),
            ModerationStatus = entity.ModerationStatus.ToString(),
            CanEdit = isAuthor && isAvailablePublic,
            CanDelete = isAuthor || IsAdmin(),
            CanReport = isCitizen && !isAuthor && isAvailablePublic && !reportedByCurrentUser,
            IsReportedByCurrentUser = reportedByCurrentUser,
            ReportCount = reportCount,
            CanModerate = _currentUser.Role is nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
