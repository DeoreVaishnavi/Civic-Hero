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
    private static readonly string[] AutoHidePatterns = ["<script", "javascript:", "buy now", "free money", "http://", "https://"];
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ComplaintCommentService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, INotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<ComplaintCommentResponse>> GetAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        var complaint = await LoadComplaintAsync(complaintId, cancellationToken);
        EnsureCanView(complaint);
        var isStaff = IsStaff();
        return complaint.Comments
            .Where(comment => isStaff || (comment.Visibility == CommentVisibility.Public && comment.ModerationStatus == CommentModerationStatus.Visible))
            .OrderBy(comment => comment.CreatedAt)
            .Select(Map).ToArray();
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
                Id = entity.Id, ComplaintId = entity.ComplaintId, ReferenceNumber = $"CH-{entity.Complaint.CreatedAt:yyyy}-{entity.ComplaintId:D6}",
                ComplaintTitle = entity.Complaint.Title, DepartmentName = entity.Complaint.Department.Name, WardName = entity.Complaint.Ward.Name,
                AuthorName = entity.User.FullName, AuthorRole = entity.User.Role.ToString(), Body = entity.Body, Visibility = entity.Visibility.ToString(),
                ModerationStatus = entity.ModerationStatus.ToString(), ModerationReason = entity.ModerationReason, CreatedAt = entity.CreatedAt
            }).ToArray(), Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<ComplaintCommentResponse> AddAsync(long complaintId, AddComplaintCommentRequest request, CancellationToken cancellationToken = default)
    {
        var complaint = await LoadComplaintAsync(complaintId, cancellationToken, tracking: true);
        EnsureCanView(complaint);
        var body = request.Body.Trim();
        if (body.Length is < 2 or > 1500) throw new ValidationException(["Comment must contain 2 to 1500 characters."]);
        var visibility = Enum.TryParse<CommentVisibility>(request.Visibility, true, out var parsed) ? parsed : CommentVisibility.Public;
        if (!IsStaff() && visibility != CommentVisibility.Public)
            throw new UnauthorizedAccessException("Citizens can only add public comments.");
        if (complaint.IsAnonymous && !IsStaff())
            throw new BusinessRuleViolationException("Anonymous complaints do not support citizen comments.");

        var hidden = AutoHidePatterns.Any(pattern => body.Contains(pattern, StringComparison.OrdinalIgnoreCase));
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
        comment.User = await _unitOfWork.Repository<User>().GetByIdAsync(comment.UserId, cancellationToken) ?? throw new NotFoundException("Comment author was not found.");

        if (!hidden && !complaint.IsAnonymous)
        {
            if (comment.UserId != complaint.CitizenId)
                await NotifyAsync(complaint.CitizenId, complaint, "New complaint comment", "A new comment was added to your complaint.", cancellationToken);
            if (complaint.AssignedOfficerId.HasValue && comment.UserId != complaint.AssignedOfficerId.Value)
                await NotifyAsync(complaint.AssignedOfficerId.Value, complaint, "New complaint comment", "A new comment was added to an assigned complaint.", cancellationToken);
        }
        return Map(comment);
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
        return Map(comment);
    }

    private async Task<Complaint> LoadComplaintAsync(long id, CancellationToken cancellationToken, bool tracking = false) =>
        await _unitOfWork.Repository<Complaint>().Query(tracking)
            .Include(entity => entity.Comments).ThenInclude(entity => entity.User)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
        ?? throw new NotFoundException("Complaint was not found.");

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
    private bool IsStaff() => _currentUser.Role is nameof(UserRole.Officer) or nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);
    private bool IsAdmin() => _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);
    private long RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
    private async Task NotifyAsync(long userId, Complaint complaint, string title, string message, CancellationToken ct) =>
        await _notifications.SendAsync(new NotificationDispatchRequest(userId, title, message, nameof(NotificationType.ComplaintProgress), "Complaint", complaint.Id, $"/citizen/complaints/{complaint.Id}"), ct);
    private ComplaintCommentResponse Map(ComplaintComment entity) => new()
    {
        Id = entity.Id, ComplaintId = entity.ComplaintId, UserId = entity.UserId,
        AuthorName = entity.User.FullName, AuthorRole = entity.User.Role.ToString(), Body = entity.Body,
        Visibility = entity.Visibility.ToString(), ModerationStatus = entity.ModerationStatus.ToString(),
        CanDelete = entity.UserId == _currentUser.UserId || IsAdmin(), CanModerate = _currentUser.Role is nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin),
        CreatedAt = entity.CreatedAt, UpdatedAt = entity.UpdatedAt
    };
}
