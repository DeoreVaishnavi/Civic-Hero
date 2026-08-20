using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Emergency;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class EmergencyReviewService : IEmergencyReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public EmergencyReviewService(IUnitOfWork unitOfWork, ICurrentUserService currentUser, INotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public ComplaintEmergencyReview CreatePendingReview(Complaint complaint, string? reason) => new()
    {
        Status = EmergencyReviewStatus.Pending,
        ReporterReason = string.IsNullOrWhiteSpace(reason) ? "Reporter marked this as a possible emergency." : reason.Trim(),
        OriginalPriority = complaint.Priority == ComplaintPriority.High ? ComplaintPriority.Medium : complaint.Priority
    };

    public async Task<PagedResponse<EmergencyReviewResponse>> GetAsync(EmergencyReviewQuery query, CancellationToken cancellationToken = default)
    {
        EnsureReviewer();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = _unitOfWork.Repository<ComplaintEmergencyReview>().Query()
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .Include(entity => entity.ReviewedByUser)
            .AsQueryable();
        source = ApplyScope(source);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<EmergencyReviewStatus>(query.Status, true, out var status))
            source = source.Where(entity => entity.Status == status);
        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(entity => entity.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponse<EmergencyReviewResponse>
        {
            Items = items.Select(Map).ToArray(), Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<EmergencyReviewResponse> DecideAsync(long reviewId, EmergencyReviewDecisionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureReviewer();
        var review = await _unitOfWork.Repository<ComplaintEmergencyReview>().Query(true)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Department)
            .Include(entity => entity.Complaint).ThenInclude(entity => entity.Ward)
            .Include(entity => entity.ReviewedByUser)
            .FirstOrDefaultAsync(entity => entity.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException("Emergency review was not found.");
        EnsureScope(review.Complaint);
        if (review.Status != EmergencyReviewStatus.Pending)
            throw new BusinessRuleViolationException("This emergency request has already been reviewed.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException(["A decision reason is required."]);

        var now = DateTimeOffset.UtcNow;
        review.ReviewedByUserId = RequireUserId();
        review.ReviewedAt = now;
        review.DecisionReason = request.Reason.Trim();
        if (string.Equals(request.Decision, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<ComplaintPriority>(request.ConfirmedPriority, true, out var priority) || priority is not (ComplaintPriority.High or ComplaintPriority.Critical))
                throw new ValidationException(["Confirmed emergency priority must be High or Critical."]);
            review.Status = EmergencyReviewStatus.Confirmed;
            review.ConfirmedPriority = priority;
            review.Complaint.Priority = priority;
            review.Complaint.EmergencyReviewStatus = EmergencyReviewStatus.Confirmed;
            review.Complaint.Timeline.Add(Timeline("EMERGENCY_CONFIRMED", $"Official review confirmed {priority} priority: {review.DecisionReason}"));
        }
        else if (string.Equals(request.Decision, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            review.Status = EmergencyReviewStatus.Rejected;
            review.ConfirmedPriority = null;
            if (review.Complaint.Priority == ComplaintPriority.High) review.Complaint.Priority = review.OriginalPriority;
            review.Complaint.EmergencyReviewStatus = EmergencyReviewStatus.Rejected;
            review.Complaint.Timeline.Add(Timeline("EMERGENCY_REJECTED", $"Possible emergency flag rejected: {review.DecisionReason}"));
        }
        else throw new ValidationException(["Decision must be Confirmed or Rejected."]);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (!review.Complaint.IsAnonymous)
        {
            await _notifications.SendAsync(new CivicHero.Backend.Core.DTOs.Notifications.NotificationDispatchRequest(
                review.Complaint.CitizenId,
                "Emergency review updated",
                $"Your complaint {Reference(review.Complaint)} emergency review is {review.Status}.",
                nameof(NotificationType.ComplaintProgress), "Complaint", review.Complaint.Id,
                $"/citizen/complaints/{review.Complaint.Id}"), cancellationToken);
        }
        return Map(review);
    }

    private IQueryable<ComplaintEmergencyReview> ApplyScope(IQueryable<ComplaintEmergencyReview> source)
    {
        if (IsAdmin()) return source;
        var departmentId = _currentUser.DepartmentId ?? -1;
        return source.Where(entity => entity.Complaint.DepartmentId == departmentId);
    }

    private void EnsureScope(Complaint complaint)
    {
        if (IsAdmin()) return;
        if (_currentUser.Role == nameof(UserRole.Supervisor) && complaint.DepartmentId == _currentUser.DepartmentId) return;
        throw new UnauthorizedAccessException("This emergency request is outside your department scope.");
    }

    private void EnsureReviewer()
    {
        if (_currentUser.Role is not (nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Supervisor or administrator permission is required.");
    }

    private bool IsAdmin() => _currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);
    private long RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
    private ComplaintTimeline Timeline(string type, string description) => new() { UserId = RequireUserId(), EventType = type, Description = description, Timestamp = DateTimeOffset.UtcNow };
    private static string Reference(Complaint complaint) => $"CH-{complaint.CreatedAt:yyyy}-{complaint.Id:D6}";
    private static EmergencyReviewResponse Map(ComplaintEmergencyReview entity) => new()
    {
        Id = entity.Id,
        ComplaintId = entity.ComplaintId,
        ReferenceNumber = Reference(entity.Complaint),
        ComplaintTitle = entity.Complaint.Title,
        DepartmentName = entity.Complaint.Department.Name,
        WardName = entity.Complaint.Ward.Name,
        Status = entity.Status.ToString(),
        ReporterReason = entity.ReporterReason,
        OriginalPriority = entity.OriginalPriority.ToString(),
        ConfirmedPriority = entity.ConfirmedPriority?.ToString(),
        ReviewedByName = entity.ReviewedByUser?.FullName,
        DecisionReason = entity.DecisionReason,
        IsAnonymous = entity.Complaint.IsAnonymous,
        RequestedAt = entity.CreatedAt,
        ReviewedAt = entity.ReviewedAt
    };
}
