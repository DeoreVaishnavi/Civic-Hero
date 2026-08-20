using CivicHero.Backend.Core.DTOs.Assignments;
using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class AssignmentService : IAssignmentService
{
    private static readonly HashSet<ComplaintStatus> InitialAssignableStatuses =
    [ComplaintStatus.Created, ComplaintStatus.AiTriage, ComplaintStatus.ReassignmentPending];

    private static readonly HashSet<ComplaintStatus> ReassignableStatuses =
    [ComplaintStatus.Assigned, ComplaintStatus.InProgress, ComplaintStatus.Escalated, ComplaintStatus.ReassignmentPending];

    private static readonly HashSet<AssignmentStatus> ActiveAssignmentStatuses =
    [AssignmentStatus.Pending, AssignmentStatus.Accepted];

    private readonly IAssignmentRepository _assignments;
    private readonly IComplaintRepository _complaints;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IStorageService _storage;
    private readonly INotificationService _notifications;
    private readonly IComplaintCommunityService _community;
    private readonly CivicDbContext _dbContext;

    public AssignmentService(
        IAssignmentRepository assignments,
        IComplaintRepository complaints,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IStorageService storage,
        INotificationService notifications,
        IComplaintCommunityService community,
        CivicDbContext dbContext)
    {
        _assignments = assignments;
        _complaints = complaints;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _storage = storage;
        _notifications = notifications;
        _community = community;
        _dbContext = dbContext;
    }

    public async Task<AssignmentDto> AssignAsync(AssignComplaintRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var assignment = await AssignCoreAsync(request.ComplaintId, request.OfficerId, request.Reason, false, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByComplaintIdAsync(assignment.ComplaintId, cancellationToken);
    }

    public async Task<IReadOnlyList<AssignmentDto>> BulkAssignAsync(BulkAssignRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var complaintIds = request.ComplaintIds.Distinct().ToArray();
        if (complaintIds.Length == 0)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["At least one complaint is required."]);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var complaintId in complaintIds)
            await AssignCoreAsync(complaintId, request.OfficerId, request.Reason, false, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var results = new List<AssignmentDto>();
        foreach (var complaintId in complaintIds)
            results.Add(await GetByComplaintIdAsync(complaintId, cancellationToken));
        return results;
    }

    public async Task<AssignmentDto> ReassignAsync(long complaintId, ReassignComplaintRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        await AssignCoreAsync(complaintId, request.OfficerId, request.Reason, true, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> AcceptAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Status != AssignmentStatus.Pending || assignment.Complaint.Status != ComplaintStatus.Assigned)
            throw new BusinessRuleViolationException("Only a pending assignment can be accepted.");

        assignment.Status = AssignmentStatus.Accepted;
        assignment.RespondedAt = DateTimeOffset.UtcNow;
        assignment.Complaint.Status = ComplaintStatus.InProgress;
        assignment.Complaint.Timeline.Add(Timeline("ASSIGNMENT_ACCEPTED", "Officer accepted the assignment."));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> RejectAsync(long complaintId, RejectAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Status != AssignmentStatus.Pending || assignment.Complaint.Status != ComplaintStatus.Assigned)
            throw new BusinessRuleViolationException("Only a pending assignment can be rejected.");

        assignment.Status = AssignmentStatus.Rejected;
        assignment.Reason = request.Reason.Trim();
        assignment.RespondedAt = DateTimeOffset.UtcNow;
        assignment.IsCurrent = false;
        assignment.Complaint.AssignedOfficerId = null;
        assignment.Complaint.Status = ComplaintStatus.ReassignmentPending;
        assignment.Complaint.Timeline.Add(Timeline("ASSIGNMENT_REJECTED", $"Officer rejected the assignment: {assignment.Reason}"));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(assignment);
    }

    public async Task<AssignmentDto> RequestTransferAsync(long complaintId, TransferAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Status is not (AssignmentStatus.Pending or AssignmentStatus.Accepted) ||
            assignment.Complaint.Status is not (ComplaintStatus.Assigned or ComplaintStatus.InProgress or ComplaintStatus.Escalated))
            throw new BusinessRuleViolationException("Only an active assignment can be transferred.");

        var now = DateTimeOffset.UtcNow;
        var reasonCode = NormalizeTransferReason(request.ReasonCode);
        var details = request.Details.Trim();
        assignment.Status = AssignmentStatus.Rejected;
        assignment.Reason = $"Transfer requested [{reasonCode}]: {details}";
        assignment.RespondedAt ??= now;
        assignment.IsCurrent = false;
        assignment.Complaint.AssignedOfficerId = null;
        assignment.Complaint.Status = ComplaintStatus.ReassignmentPending;
        assignment.Complaint.Timeline.Add(Timeline("OFFICER_TRANSFER_REQUESTED", assignment.Reason));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var supervisorIds = await _dbContext.Users.AsNoTracking()
            .Where(user => user.IsActive && !user.IsDeleted &&
                ((user.Role == UserRole.Supervisor && user.DepartmentId == assignment.Complaint.DepartmentId &&
                    (!user.WardId.HasValue || user.WardId == assignment.Complaint.WardId)) ||
                 user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin))
            .Select(user => user.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var supervisorId in supervisorIds)
        {
            await _notifications.SendAsync(new NotificationDispatchRequest(
                supervisorId,
                "Officer transfer request",
                $"{assignment.Complaint.Title}: {reasonCode} — {details}",
                nameof(NotificationType.ComplaintAssigned),
                "Complaint",
                complaintId,
                $"/supervisor/assignments/{complaintId}"), cancellationToken);
        }

        return Map(assignment);
    }

    public async Task<AssignmentDto> AddProgressAsync(long complaintId, AddProgressRequest request, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        ValidateCoordinates(request.Latitude, request.Longitude);
        ValidateEstimatedCompletion(request.EstimatedCompletionAt);
        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, true, cancellationToken);
        EnsureProgressAllowed(assignment);

        var now = DateTimeOffset.UtcNow;
        var update = new ComplaintProgressUpdate
        {
            ComplaintId = complaintId,
            OfficerId = RequireUserId(),
            Message = request.Message.Trim(),
            ProgressPercent = request.ProgressPercent,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = now
        };
        assignment.Complaint.ProgressUpdates.Add(update);
        assignment.Complaint.Status = ComplaintStatus.InProgress;
        assignment.Complaint.Timeline.Add(Timeline("PROGRESS_UPDATED", $"Work progress updated to {request.ProgressPercent}%: {request.Message.Trim()}"));
        if (request.EstimatedCompletionAt.HasValue)
            assignment.Complaint.Timeline.Add(Timeline("OFFICER_ESTIMATED_COMPLETION", request.EstimatedCompletionAt.Value.ToUniversalTime().ToString("O")));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _community.NotifyFollowersAsync(
            complaintId,
            "Progress update on a followed complaint",
            $"Work progress reached {request.ProgressPercent}%: {request.Message.Trim()}",
            RequireUserId(),
            cancellationToken: cancellationToken);
        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> AddProgressWithEvidenceAsync(long complaintId, AddProgressEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        ValidateCoordinates(request.Latitude, request.Longitude);
        ValidateEstimatedCompletion(request.EstimatedCompletionAt);
        if (request.Evidence.Count is < 1 or > 5)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Upload between one and five progress evidence files."]);
        if (request.Evidence.Sum(file => file.Length) > 50L * 1024 * 1024)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Progress evidence cannot exceed 50 MB in total."]);

        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, true, cancellationToken);
        EnsureProgressAllowed(assignment);
        var now = DateTimeOffset.UtcNow;
        var update = new ComplaintProgressUpdate
        {
            ComplaintId = complaintId,
            OfficerId = RequireUserId(),
            Message = request.Message.Trim(),
            ProgressPercent = request.ProgressPercent,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = now
        };

        var uploadedKeys = new List<string>();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            assignment.Complaint.ProgressUpdates.Add(update);
            assignment.Complaint.Status = ComplaintStatus.InProgress;
            assignment.Complaint.Timeline.Add(Timeline("PROGRESS_UPDATED", $"Work progress updated to {request.ProgressPercent}% with {request.Evidence.Count} evidence file(s): {request.Message.Trim()}"));
            if (request.EstimatedCompletionAt.HasValue)
                assignment.Complaint.Timeline.Add(Timeline("OFFICER_ESTIMATED_COMPLETION", request.EstimatedCompletionAt.Value.ToUniversalTime().ToString("O")));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var file in request.Evidence)
            {
                var extension = await ValidateOfficerEvidenceAsync(file, cancellationToken);
                var objectKey = $"progress/{complaintId}/{update.Id}/{Guid.NewGuid():N}{extension}";
                await using var stream = file.OpenReadStream();
                await _storage.UploadAsync(stream, objectKey, file.ContentType,
                    new Dictionary<string, string>
                    {
                        ["complaint-id"] = complaintId.ToString(),
                        ["officer-id"] = RequireUserId().ToString(),
                        ["progress-update-id"] = update.Id.ToString(),
                        ["evidence-type"] = "progress"
                    }, cancellationToken);

                uploadedKeys.Add(objectKey);
                assignment.Complaint.Images.Add(new ComplaintImage
                {
                    S3Key = objectKey,
                    FileName = Path.GetFileName(file.FileName),
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    IsResolutionEvidence = false,
                    UploadedAt = DateTimeOffset.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _community.NotifyFollowersAsync(
                complaintId,
                "Progress evidence added to a followed complaint",
                $"Work progress reached {request.ProgressPercent}% with new evidence.",
                RequireUserId(),
                cancellationToken: cancellationToken);
            return await GetByComplaintIdAsync(complaintId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            foreach (var key in uploadedKeys)
            {
                try { await _storage.DeleteAsync(key, cancellationToken); }
                catch { }
            }
            throw;
        }
    }

    public async Task<AssignmentDto> RequestCitizenInformationAsync(long complaintId, OfficerCitizenInformationRequest request, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, false, cancellationToken);
        EnsureProgressAllowed(assignment);
        var message = request.Message.Trim();
        await _dbContext.AuditLogs.AddAsync(new AuditLog
        {
            UserId = RequireUserId(),
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = "OFFICER_CITIZEN_INFORMATION_REQUESTED",
            EntityName = "Complaint",
            EntityId = complaintId.ToString(),
            NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { message, citizenId = assignment.Complaint.CitizenId }),
            Severity = "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            assignment.Complaint.CitizenId,
            "Officer requested additional information",
            message,
            nameof(NotificationType.ComplaintProgress),
            "Complaint",
            complaintId,
            $"/citizen/complaints/{complaintId}"), cancellationToken);

        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> CompleteAsync(long complaintId, CompleteAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        ValidateCoordinates(request.Latitude, request.Longitude);
        var assignment = await RequireCurrentOfficerAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Status != AssignmentStatus.Accepted || assignment.Complaint.Status is not (ComplaintStatus.InProgress or ComplaintStatus.Escalated))
            throw new BusinessRuleViolationException("Only accepted work can be completed.");
        if (request.Evidence.Count is < 1 or > 5)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Upload between one and five completion evidence files."]);
        if (request.Evidence.Sum(file => file.Length) > 50L * 1024 * 1024)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Completion evidence cannot exceed 50 MB in total."]);
        if (!request.Evidence.Any(file => file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["At least one JPEG, PNG, or WebP completion image is required."]);

        var uploadedKeys = new List<string>();
        try
        {
            foreach (var file in request.Evidence)
            {
                var extension = await ValidateOfficerEvidenceAsync(file, cancellationToken);
                var objectKey = $"resolutions/{complaintId}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
                await using var stream = file.OpenReadStream();
                await _storage.UploadAsync(stream, objectKey, file.ContentType,
                    new Dictionary<string, string>
                    {
                        ["complaint-id"] = complaintId.ToString(),
                        ["officer-id"] = RequireUserId().ToString(),
                        ["evidence-type"] = "resolution"
                    }, cancellationToken);

                uploadedKeys.Add(objectKey);
                assignment.Complaint.Images.Add(new ComplaintImage
                {
                    S3Key = objectKey,
                    FileName = Path.GetFileName(file.FileName),
                    FileSize = file.Length,
                    MimeType = file.ContentType,
                    IsResolutionEvidence = true,
                    UploadedAt = DateTimeOffset.UtcNow
                });
            }

            var now = DateTimeOffset.UtcNow;
            assignment.Status = AssignmentStatus.Completed;
            assignment.RespondedAt ??= now;
            assignment.CompletedAt = now;
            assignment.Complaint.ResolvedAt = now;
            assignment.Complaint.Status = ComplaintStatus.VerificationPending;
            assignment.Complaint.ProgressUpdates.Add(new ComplaintProgressUpdate
            {
                ComplaintId = complaintId,
                OfficerId = RequireUserId(),
                Message = request.Notes.Trim(),
                ProgressPercent = 100,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CreatedAt = now
            });
            assignment.Complaint.Timeline.Add(Timeline("RESOLUTION_SUBMITTED", $"Officer submitted resolution evidence: {request.Notes.Trim()}"));
            assignment.Complaint.Timeline.Add(Timeline("VERIFICATION_PENDING", "Resolution is ready for citizen verification."));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _community.NotifyFollowersAsync(
                complaintId,
                "Resolution submitted for a followed complaint",
                "The Officer submitted resolution evidence and the complaint is awaiting Citizen verification.",
                RequireUserId(),
                cancellationToken: cancellationToken);
            return await GetByComplaintIdAsync(complaintId, cancellationToken);
        }
        catch
        {
            foreach (var key in uploadedKeys)
            {
                try { await _storage.DeleteAsync(key, cancellationToken); }
                catch { }
            }
            throw;
        }
    }

    public async Task<AssignmentDto> EscalateAsync(long complaintId, SupervisorActionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var assignment = await RequireScopedAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Complaint.Status is not (ComplaintStatus.Assigned or ComplaintStatus.InProgress))
            throw new BusinessRuleViolationException("Only assigned or in-progress work can be manually escalated.");

        assignment.Complaint.Status = ComplaintStatus.Escalated;
        assignment.Complaint.Timeline.Add(Timeline("MANUAL_ESCALATION", $"Supervisor escalated the complaint: {request.Reason.Trim()}"));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            assignment.OfficerId,
            "Complaint escalated",
            request.Reason.Trim(),
            nameof(NotificationType.SlaEscalation),
            "Complaint",
            complaintId,
            $"/officer/assignments/{complaintId}"), cancellationToken);

        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> ResumeEscalatedAsync(long complaintId, SupervisorActionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var assignment = await RequireScopedAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Complaint.Status != ComplaintStatus.Escalated)
            throw new BusinessRuleViolationException("Only an escalated complaint can be returned to in-progress work.");

        assignment.Complaint.Status = ComplaintStatus.InProgress;
        assignment.Complaint.Timeline.Add(Timeline("ESCALATION_RESUMED", $"Supervisor returned the complaint to in-progress work: {request.Reason.Trim()}"));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> RequestReworkAsync(long complaintId, SupervisorActionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var assignment = await RequireScopedAssignmentAsync(complaintId, true, cancellationToken);
        if (assignment.Complaint.Status is ComplaintStatus.Withdrawn or ComplaintStatus.Merged or ComplaintStatus.ClosedFraud)
            throw new BusinessRuleViolationException("This complaint cannot be returned for rework.");

        assignment.Status = AssignmentStatus.Accepted;
        assignment.IsCurrent = true;
        assignment.CompletedAt = null;
        assignment.RespondedAt ??= DateTimeOffset.UtcNow;
        assignment.Complaint.Status = ComplaintStatus.InProgress;
        assignment.Complaint.ResolvedAt = null;
        assignment.Complaint.ClosedAt = null;
        assignment.Complaint.Timeline.Add(Timeline("SUPERVISOR_REWORK_REQUESTED", $"Supervisor requested rework: {request.Reason.Trim()}"));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            assignment.OfficerId,
            "Rework requested",
            request.Reason.Trim(),
            nameof(NotificationType.ComplaintProgress),
            "Complaint",
            complaintId,
            $"/officer/assignments/{complaintId}"), cancellationToken);

        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> SendOfficerInstructionAsync(long complaintId, SupervisorMessageRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var assignment = await RequireScopedAssignmentAsync(complaintId, true, cancellationToken);
        assignment.Complaint.Timeline.Add(Timeline("SUPERVISOR_INSTRUCTION", $"Instruction to officer: {request.Message.Trim()}"));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            assignment.OfficerId,
            "Supervisor instruction",
            request.Message.Trim(),
            nameof(NotificationType.ComplaintProgress),
            "Complaint",
            complaintId,
            $"/officer/assignments/{complaintId}"), cancellationToken);

        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<AssignmentDto> RequestCitizenEvidenceAsync(long complaintId, SupervisorMessageRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var assignment = await RequireScopedAssignmentAsync(complaintId, true, cancellationToken);
        assignment.Complaint.Timeline.Add(Timeline("CITIZEN_EVIDENCE_REQUESTED", $"Supervisor requested additional citizen evidence: {request.Message.Trim()}"));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            assignment.Complaint.CitizenId,
            "Additional evidence requested",
            request.Message.Trim(),
            nameof(NotificationType.VerificationRequired),
            "Complaint",
            complaintId,
            "/citizen/verifications"), cancellationToken);

        return await GetByComplaintIdAsync(complaintId, cancellationToken);
    }

    public async Task<IReadOnlyList<EscalationHistoryDto>> GetEscalationHistoryAsync(AssignmentQuery query, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var eventTypes = new[] { "MANUAL_ESCALATION", "ESCALATION_RESUMED", "SLA_ESCALATED", "SLA_BREACH", "ADMIN_ESCALATION", "SUPERVISOR_REWORK_REQUESTED", "DISPUTE_ESCALATED_TO_SUPERADMIN" };
        var source = _dbContext.ComplaintTimelines.AsNoTracking()
            .Include(x => x.Complaint)
            .Include(x => x.User)
            .Where(x => eventTypes.Contains(x.EventType));

        if (_currentUser.Role == nameof(UserRole.Supervisor))
        {
            var departmentId = _currentUser.DepartmentId ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
            source = source.Where(x => x.Complaint.DepartmentId == departmentId);
            if (_currentUser.WardId.HasValue) source = source.Where(x => x.Complaint.WardId == _currentUser.WardId.Value);
        }
        else
        {
            if (query.DepartmentId.HasValue) source = source.Where(x => x.Complaint.DepartmentId == query.DepartmentId.Value);
            if (query.WardId.HasValue) source = source.Where(x => x.Complaint.WardId == query.WardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category)) source = source.Where(x => x.Complaint.Category == query.Category.Trim());
        if (query.FromDate.HasValue) source = source.Where(x => x.Timestamp >= query.FromDate.Value);
        if (query.ToDate.HasValue) source = source.Where(x => x.Timestamp <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.Complaint.Title.Contains(search) || x.Description.Contains(search));
        }

        var records = await source.OrderByDescending(x => x.Timestamp).Take(300)
            .Select(x => new
            {
                x.Id,
                x.ComplaintId,
                x.Complaint.Title,
                x.EventType,
                x.Description,
                ActorName = x.User == null ? null : x.User.FullName,
                x.Timestamp
            }).ToListAsync(cancellationToken);

        return records.Select(x => new EscalationHistoryDto
        {
            TimelineId = x.Id,
            ComplaintId = x.ComplaintId,
            ReferenceNumber = "CH-" + x.ComplaintId.ToString("D6"),
            Title = x.Title,
            EventType = x.EventType,
            Description = x.Description,
            ActorName = x.ActorName,
            Timestamp = x.Timestamp
        }).ToArray();
    }

    public async Task<PagedResponse<AssignmentDto>> GetMyAssignmentsAsync(AssignmentQuery query, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var userId = RequireUserId();
        var source = _assignments.QueryWithDetails().Where(entity => entity.OfficerId == userId && entity.IsCurrent);
        return await PageAsync(ApplyFilters(source, query), query, cancellationToken);
    }

    public async Task<PagedResponse<AssignmentDto>> GetPendingAsync(AssignmentQuery query, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var complaintSource = ApplySupervisorScope(_complaints.QueryWithSummary())
            .Where(entity => entity.Status == ComplaintStatus.Created || entity.Status == ComplaintStatus.ReassignmentPending);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            complaintSource = complaintSource.Where(entity => entity.Title.Contains(search) || entity.Description.Contains(search) || entity.Address.Contains(search));
        }
        if (query.DepartmentId.HasValue) complaintSource = complaintSource.Where(entity => entity.DepartmentId == query.DepartmentId.Value);
        if (query.WardId.HasValue) complaintSource = complaintSource.Where(entity => entity.WardId == query.WardId.Value);
        if (!string.IsNullOrWhiteSpace(query.Category)) complaintSource = complaintSource.Where(entity => entity.Category == query.Category.Trim());
        if (query.FromDate.HasValue) complaintSource = complaintSource.Where(entity => entity.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) complaintSource = complaintSource.Where(entity => entity.CreatedAt <= query.ToDate.Value);
        if (Enum.TryParse<ComplaintPriority>(query.Priority, true, out var priority)) complaintSource = complaintSource.Where(entity => entity.Priority == priority);
        complaintSource = ApplyPendingSlaRisk(complaintSource, query.SlaRisk);

        var total = await complaintSource.CountAsync(cancellationToken);
        var complaints = await complaintSource.OrderByDescending(entity => entity.Priority).ThenBy(entity => entity.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var items = complaints.Select(MapUnassigned).ToArray();
        return new PagedResponse<AssignmentDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<IReadOnlyList<AssignmentDto>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var now = DateTimeOffset.UtcNow;
        var source = ApplySupervisorScope(_assignments.QueryWithDetails())
            .Where(entity => entity.IsCurrent &&
                ((entity.Status == AssignmentStatus.Pending && entity.AssignmentDueAt < now) ||
                 (entity.Status == AssignmentStatus.Accepted && entity.ResolutionDueAt < now)));
        return (await source.OrderBy(entity => entity.AssignmentDueAt).Take(100).ToListAsync(cancellationToken)).Select(Map).ToArray();
    }

    public async Task<AssignmentDto> GetByComplaintIdAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetCurrentByComplaintIdAsync(complaintId, false, cancellationToken)
            ?? throw new NotFoundException("No current assignment exists for this complaint.");
        EnsureCanView(assignment);
        return Map(assignment);
    }

    public async Task<IReadOnlyList<AssignmentHistoryDto>> GetHistoryAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        var current = await _assignments.GetCurrentByComplaintIdAsync(complaintId, false, cancellationToken);
        if (current is not null) EnsureCanView(current);
        else
        {
            var complaint = await _complaints.QueryWithSummary().FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
                ?? throw new NotFoundException("Complaint was not found.");
            EnsureComplaintScope(complaint);
        }

        return await _assignments.QueryWithDetails()
            .Where(entity => entity.ComplaintId == complaintId)
            .OrderByDescending(entity => entity.AssignedAt)
            .Select(entity => new AssignmentHistoryDto
            {
                Id = entity.Id,
                OfficerId = entity.OfficerId,
                OfficerName = entity.Officer.FullName,
                AssignedByName = entity.AssignedBy.FullName,
                Status = entity.Status.ToString(),
                Reason = entity.Reason,
                AssignedAt = entity.AssignedAt,
                RespondedAt = entity.RespondedAt,
                ReassignedAt = entity.ReassignedAt,
                CompletedAt = entity.CompletedAt,
                AssignmentDueAt = entity.AssignmentDueAt,
                ResolutionDueAt = entity.ResolutionDueAt,
                IsCurrent = entity.IsCurrent
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfficerWorkloadDto>> GetWorkloadAsync(long? departmentId, long? wardId, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        IQueryable<User> officers = _unitOfWork.Repository<User>().Query()
            .Where(entity => entity.Role == UserRole.Officer && entity.IsActive)
            .Include(entity => entity.Department).Include(entity => entity.Ward);
        officers = ApplyOfficerScope(officers, departmentId, wardId);

        var officerList = await officers.OrderBy(entity => entity.FullName).ToListAsync(cancellationToken);
        var officerIds = officerList.Select(entity => entity.Id).ToArray();
        var assignments = await _assignments.Query()
            .Where(entity => officerIds.Contains(entity.OfficerId))
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var weekStart = now.AddDays(-7);

        return officerList.Select(officer =>
        {
            var own = assignments.Where(item => item.OfficerId == officer.Id).ToArray();
            return new OfficerWorkloadDto
            {
                OfficerId = officer.Id,
                OfficerName = officer.FullName,
                Email = officer.Email,
                DepartmentId = officer.DepartmentId,
                DepartmentName = officer.Department?.Name,
                WardId = officer.WardId,
                WardName = officer.Ward?.Name,
                PendingCount = own.Count(item => item.IsCurrent && item.Status == AssignmentStatus.Pending),
                InProgressCount = own.Count(item => item.IsCurrent && item.Status == AssignmentStatus.Accepted),
                OverdueCount = own.Count(item => item.IsCurrent && ((item.Status == AssignmentStatus.Pending && item.AssignmentDueAt < now) || (item.Status == AssignmentStatus.Accepted && item.ResolutionDueAt < now))),
                CompletedCount = own.Count(item => item.Status == AssignmentStatus.Completed && item.CompletedAt >= weekStart)
            };
        }).OrderBy(item => item.ActiveWorkload).ThenBy(item => item.OverdueCount).ToArray();
    }

    public async Task<IReadOnlyList<EligibleOfficerDto>> GetEligibleOfficersAsync(long complaintId, CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var complaint = await _complaints.QueryWithSummary().FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        EnsureSupervisorScope(complaint);

        var workload = await GetWorkloadAsync(complaint.DepartmentId, complaint.WardId, cancellationToken);
        return workload.Select((item, index) => new EligibleOfficerDto
        {
            OfficerId = item.OfficerId,
            OfficerName = item.OfficerName,
            Email = item.Email,
            DepartmentName = item.DepartmentName ?? "Department",
            WardName = item.WardName ?? "Ward",
            ActiveWorkload = item.ActiveWorkload,
            OverdueCount = item.OverdueCount,
            Recommendation = index == 0 ? "Best available" : item.OverdueCount > 0 ? "Has overdue work" : "Available"
        }).ToArray();
    }

    public async Task<AssignmentDashboardResponse> GetOfficerDashboardAsync(CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var userId = RequireUserId();
        var now = DateTimeOffset.UtcNow;
        var weekStart = now.AddDays(-7);
        var source = _assignments.QueryWithDetails().Where(entity => entity.OfficerId == userId && entity.IsCurrent);
        var items = await source.ToListAsync(cancellationToken);
        var completedThisWeek = await _assignments.Query().CountAsync(entity => entity.OfficerId == userId && entity.Status == AssignmentStatus.Completed && entity.CompletedAt >= weekStart, cancellationToken);
        return Dashboard(items, completedThisWeek, 0, 0);
    }

    public async Task<OfficerPerformanceDto> GetOfficerPerformanceAsync(int days, CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var rangeDays = Math.Clamp(days, 7, 365);
        var to = DateTimeOffset.UtcNow;
        var from = to.AddDays(-rangeDays);
        var userId = RequireUserId();

        var assignments = await _dbContext.ComplaintAssignments.AsNoTracking()
            .Where(entity => entity.OfficerId == userId && entity.AssignedAt >= from && entity.AssignedAt <= to)
            .ToListAsync(cancellationToken);

        var accepted = assignments.Where(entity => entity.RespondedAt.HasValue && entity.Status != AssignmentStatus.Rejected).ToArray();
        var completed = assignments.Where(entity => entity.CompletedAt.HasValue || entity.Status == AssignmentStatus.Completed).ToArray();
        var completedIds = completed.Select(entity => entity.ComplaintId).Distinct().ToArray();
        var verificationRows = completedIds.Length == 0
            ? new List<ComplaintVerification>()
            : await _dbContext.ComplaintVerifications.AsNoTracking()
                .Where(entity => completedIds.Contains(entity.ComplaintId) && entity.CompletedAt.HasValue)
                .OrderByDescending(entity => entity.CompletedAt)
                .ToListAsync(cancellationToken);
        var latestVerifications = verificationRows
            .GroupBy(entity => entity.ComplaintId)
            .Select(group => group.First())
            .ToArray();
        var verificationSuccesses = latestVerifications.Count(entity => entity.Decision is VerificationDecision.Approved or VerificationDecision.AutoClosed or VerificationDecision.AdminOverride);

        var averageResponse = assignments.Where(entity => entity.RespondedAt.HasValue)
            .Select(entity => (entity.RespondedAt!.Value - entity.AssignedAt).TotalMinutes)
            .DefaultIfEmpty(0d).Average();
        var averageResolution = completed.Where(entity => entity.CompletedAt.HasValue)
            .Select(entity => (entity.CompletedAt!.Value - (entity.RespondedAt ?? entity.AssignedAt)).TotalHours)
            .DefaultIfEmpty(0d).Average();
        var slaCompliant = completed.Count(entity => entity.CompletedAt.HasValue && entity.CompletedAt.Value <= entity.ResolutionDueAt);
        var active = assignments.Count(entity => entity.IsCurrent && entity.Status is AssignmentStatus.Pending or AssignmentStatus.Accepted);
        var transfers = assignments.Count(entity => entity.Status == AssignmentStatus.Rejected &&
            entity.Reason != null && entity.Reason.StartsWith("Transfer requested ["));

        return new OfficerPerformanceDto
        {
            RangeDays = rangeDays,
            TotalAssignments = assignments.Count,
            AcceptedAssignments = accepted.Length,
            CompletedAssignments = completed.Length,
            ActiveAssignments = active,
            TransferRequests = transfers,
            AverageResponseMinutes = Math.Round((decimal)averageResponse, 1),
            AverageResolutionHours = Math.Round((decimal)averageResolution, 1),
            ClosureRatePercent = accepted.Length == 0 ? 0 : Math.Round(completed.Length * 100m / accepted.Length, 1),
            VerificationSuccessRatePercent = latestVerifications.Length == 0 ? 0 : Math.Round(verificationSuccesses * 100m / latestVerifications.Length, 1),
            SlaCompliancePercent = completed.Length == 0 ? 100 : Math.Round(slaCompliant * 100m / completed.Length, 1),
            From = from,
            To = to
        };
    }

    public async Task<IReadOnlyList<OfficerMapItemDto>> GetOfficerMapAsync(CancellationToken cancellationToken = default)
    {
        EnsureOfficer();
        var userId = RequireUserId();
        var assignments = await _assignments.QueryWithDetails()
            .Where(entity => entity.OfficerId == userId && entity.IsCurrent &&
                (entity.Status == AssignmentStatus.Pending || entity.Status == AssignmentStatus.Accepted))
            .OrderBy(entity => entity.ResolutionDueAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return assignments.Select(entity =>
        {
            var mapped = Map(entity);
            return new OfficerMapItemDto
            {
                ComplaintId = mapped.ComplaintId,
                ReferenceNumber = mapped.ReferenceNumber,
                Title = mapped.Title,
                Category = mapped.Category,
                Priority = mapped.Priority,
                ComplaintStatus = mapped.ComplaintStatus,
                AssignmentStatus = mapped.AssignmentStatus,
                SlaState = mapped.SlaState,
                RemainingMinutes = mapped.RemainingMinutes,
                Address = mapped.Address,
                WardName = mapped.WardName,
                Latitude = mapped.Latitude,
                Longitude = mapped.Longitude,
                EstimatedCompletionAt = mapped.EstimatedCompletionAt
            };
        }).ToArray();
    }

    public async Task<AssignmentDashboardResponse> GetSupervisorDashboardAsync(CancellationToken cancellationToken = default)
    {
        EnsureSupervisorOrAbove();
        var now = DateTimeOffset.UtcNow;
        var weekStart = now.AddDays(-7);
        var assignments = await ApplySupervisorScope(_assignments.QueryWithDetails()).Where(entity => entity.IsCurrent).ToListAsync(cancellationToken);
        var completedThisWeek = await ApplySupervisorScope(_assignments.QueryWithDetails())
            .CountAsync(entity => entity.Status == AssignmentStatus.Completed && entity.CompletedAt >= weekStart, cancellationToken);
        var unassigned = await ApplySupervisorScope(_complaints.QueryWithSummary()).CountAsync(entity => entity.Status == ComplaintStatus.Created, cancellationToken);
        var reassign = await ApplySupervisorScope(_complaints.QueryWithSummary()).CountAsync(entity => entity.Status == ComplaintStatus.ReassignmentPending, cancellationToken);
        return Dashboard(assignments, completedThisWeek, unassigned, reassign);
    }

    private async Task<ComplaintAssignment> AssignCoreAsync(long complaintId, long officerId, string? reason, bool isReassignment, CancellationToken cancellationToken)
    {
        var complaint = await _complaints.GetDetailsAsync(complaintId, true, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        EnsureSupervisorScope(complaint);

        var allowed = isReassignment ? ReassignableStatuses : InitialAssignableStatuses;
        if (!allowed.Contains(complaint.Status))
            throw new BusinessRuleViolationException(isReassignment ? "This complaint cannot be reassigned in its current state." : "This complaint is not ready for assignment.");

        var officer = await _unitOfWork.Repository<User>().Query(true)
            .Include(entity => entity.Department).Include(entity => entity.Ward)
            .FirstOrDefaultAsync(entity => entity.Id == officerId, cancellationToken)
            ?? throw new NotFoundException("Officer was not found.");
        ValidateOfficer(officer, complaint);

        var current = await _assignments.GetCurrentByComplaintIdAsync(complaintId, true, cancellationToken);
        if (current is not null)
        {
            if (!isReassignment && ActiveAssignmentStatuses.Contains(current.Status))
                throw new ConflictException("This complaint already has an active assignment.");
            current.IsCurrent = false;
            current.Status = AssignmentStatus.Reassigned;
            current.ReassignedAt = DateTimeOffset.UtcNow;
            current.Reason = string.IsNullOrWhiteSpace(reason) ? "Reassigned by supervisor." : reason.Trim();
        }

        var now = DateTimeOffset.UtcNow;
        var (assignmentWindow, resolutionWindow) = GetSla(complaint.Priority);
        var assignment = new ComplaintAssignment
        {
            ComplaintId = complaint.Id,
            OfficerId = officer.Id,
            AssignedById = RequireUserId(),
            Status = AssignmentStatus.Pending,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            AssignedAt = now,
            AssignmentDueAt = (isReassignment ? now : complaint.CreatedAt).Add(assignmentWindow),
            ResolutionDueAt = now.Add(resolutionWindow),
            IsCurrent = true
        };
        await _assignments.AddAsync(assignment, cancellationToken);
        complaint.AssignedOfficerId = officer.Id;
        complaint.Status = ComplaintStatus.Assigned;
        complaint.Timeline.Add(Timeline(isReassignment ? "REASSIGNED" : "ASSIGNED", $"Assigned to {officer.FullName}."));
        return assignment;
    }

    private async Task<ComplaintAssignment> RequireScopedAssignmentAsync(long complaintId, bool asTracking, CancellationToken cancellationToken)
    {
        var assignment = await _assignments.GetCurrentByComplaintIdAsync(complaintId, asTracking, cancellationToken)
            ?? throw new NotFoundException("No current assignment exists for this complaint.");
        EnsureSupervisorScope(assignment.Complaint);
        return assignment;
    }

    private static IQueryable<Complaint> ApplyPendingSlaRisk(IQueryable<Complaint> source, string? slaRisk)
    {
        if (string.IsNullOrWhiteSpace(slaRisk)) return source;
        var now = DateTimeOffset.UtcNow;
        if (string.Equals(slaRisk, "Breached", StringComparison.OrdinalIgnoreCase))
            return source.Where(x =>
                (x.Priority == ComplaintPriority.Low && x.CreatedAt < now.AddHours(-48)) ||
                (x.Priority == ComplaintPriority.Medium && x.CreatedAt < now.AddHours(-24)) ||
                (x.Priority == ComplaintPriority.High && x.CreatedAt < now.AddHours(-12)) ||
                (x.Priority == ComplaintPriority.Critical && x.CreatedAt < now.AddHours(-2)));
        if (string.Equals(slaRisk, "AtRisk", StringComparison.OrdinalIgnoreCase))
            return source.Where(x =>
                (x.Priority == ComplaintPriority.Low && x.CreatedAt >= now.AddHours(-48) && x.CreatedAt <= now.AddHours(-36)) ||
                (x.Priority == ComplaintPriority.Medium && x.CreatedAt >= now.AddHours(-24) && x.CreatedAt <= now.AddHours(-18)) ||
                (x.Priority == ComplaintPriority.High && x.CreatedAt >= now.AddHours(-12) && x.CreatedAt <= now.AddHours(-9)) ||
                (x.Priority == ComplaintPriority.Critical && x.CreatedAt >= now.AddHours(-2) && x.CreatedAt <= now.AddMinutes(-90)));
        if (string.Equals(slaRisk, "OnTrack", StringComparison.OrdinalIgnoreCase))
            return source.Where(x =>
                (x.Priority == ComplaintPriority.Low && x.CreatedAt > now.AddHours(-36)) ||
                (x.Priority == ComplaintPriority.Medium && x.CreatedAt > now.AddHours(-18)) ||
                (x.Priority == ComplaintPriority.High && x.CreatedAt > now.AddHours(-9)) ||
                (x.Priority == ComplaintPriority.Critical && x.CreatedAt > now.AddMinutes(-90)));
        return source;
    }

    private async Task<ComplaintAssignment> RequireCurrentOfficerAssignmentAsync(long complaintId, bool asTracking, CancellationToken cancellationToken)
    {
        var assignment = await _assignments.GetCurrentByComplaintIdAsync(complaintId, asTracking, cancellationToken)
            ?? throw new NotFoundException("No current assignment exists for this complaint.");
        if (assignment.OfficerId != RequireUserId())
            throw new UnauthorizedAccessException("This assignment belongs to another officer.");
        return assignment;
    }

    private IQueryable<ComplaintAssignment> ApplyFilters(IQueryable<ComplaintAssignment> source, AssignmentQuery query)
    {
        if (Enum.TryParse<AssignmentStatus>(query.Status, true, out var status)) source = source.Where(entity => entity.Status == status);
        if (Enum.TryParse<ComplaintPriority>(query.Priority, true, out var priority)) source = source.Where(entity => entity.Complaint.Priority == priority);
        if (query.DepartmentId.HasValue) source = source.Where(entity => entity.Complaint.DepartmentId == query.DepartmentId.Value);
        if (query.WardId.HasValue) source = source.Where(entity => entity.Complaint.WardId == query.WardId.Value);
        if (!string.IsNullOrWhiteSpace(query.Category)) source = source.Where(entity => entity.Complaint.Category == query.Category.Trim());
        if (query.FromDate.HasValue) source = source.Where(entity => entity.Complaint.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) source = source.Where(entity => entity.Complaint.CreatedAt <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(entity => entity.Complaint.Title.Contains(search) || entity.Complaint.Description.Contains(search) || entity.Complaint.Address.Contains(search));
        }
        var now = DateTimeOffset.UtcNow;
        if (query.OverdueOnly || string.Equals(query.SlaRisk, "Breached", StringComparison.OrdinalIgnoreCase))
            source = source.Where(entity => (entity.Status == AssignmentStatus.Pending && entity.AssignmentDueAt < now) || (entity.Status == AssignmentStatus.Accepted && entity.ResolutionDueAt < now));
        else if (string.Equals(query.SlaRisk, "AtRisk", StringComparison.OrdinalIgnoreCase))
            source = source.Where(entity =>
                (entity.Status == AssignmentStatus.Pending && entity.AssignmentDueAt >= now && entity.AssignmentDueAt <= now.AddHours(6)) ||
                (entity.Status == AssignmentStatus.Accepted && entity.ResolutionDueAt >= now && entity.ResolutionDueAt <= now.AddHours(12)));
        else if (string.Equals(query.SlaRisk, "OnTrack", StringComparison.OrdinalIgnoreCase))
            source = source.Where(entity =>
                (entity.Status == AssignmentStatus.Pending && entity.AssignmentDueAt > now.AddHours(6)) ||
                (entity.Status == AssignmentStatus.Accepted && entity.ResolutionDueAt > now.AddHours(12)));
        return source;
    }

    private async Task<PagedResponse<AssignmentDto>> PageAsync(IQueryable<ComplaintAssignment> source, AssignmentQuery query, CancellationToken cancellationToken)
    {
        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(entity => entity.Complaint.Priority).ThenBy(entity => entity.AssignmentDueAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResponse<AssignmentDto> { Items = items.Select(Map).ToArray(), Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    private AssignmentDashboardResponse Dashboard(IReadOnlyList<ComplaintAssignment> assignments, int completedThisWeek, int unassigned, int reassign)
    {
        var now = DateTimeOffset.UtcNow;
        var active = assignments.Where(entity => ActiveAssignmentStatuses.Contains(entity.Status)).ToArray();
        var overdue = active.Count(entity => (entity.Status == AssignmentStatus.Pending && entity.AssignmentDueAt < now) || (entity.Status == AssignmentStatus.Accepted && entity.ResolutionDueAt < now));
        var completed = assignments.Count(entity => entity.Status == AssignmentStatus.Completed);
        var complianceBase = completed + overdue;
        var compliance = complianceBase == 0 ? 100m : Math.Round(completed * 100m / complianceBase, 1);
        return new AssignmentDashboardResponse
        {
            Pending = assignments.Count(entity => entity.Status == AssignmentStatus.Pending),
            InProgress = assignments.Count(entity => entity.Status == AssignmentStatus.Accepted),
            CompletedThisWeek = completedThisWeek,
            Overdue = overdue,
            Unassigned = unassigned,
            ReassignmentPending = reassign,
            SlaCompliancePercent = compliance,
            PriorityItems = active.OrderBy(entity => DueAt(entity)).Take(6).Select(Map).ToArray()
        };
    }

    private AssignmentDto Map(ComplaintAssignment assignment)
    {
        var now = DateTimeOffset.UtcNow;
        var due = DueAt(assignment);
        var remaining = (long)Math.Floor((due - now).TotalMinutes);
        var totalWindow = Math.Max(1d, (due - assignment.AssignedAt).TotalMinutes);
        var remainingRatio = remaining / totalWindow;
        var slaState = remaining < 0 ? "Breached" : remainingRatio <= 0.25 ? "AtRisk" : "OnTrack";
        var role = _currentUser.Role ?? string.Empty;
        var currentUserId = _currentUser.UserId;
        var officerAction = currentUserId == assignment.OfficerId;
        var supervisorAction = role is nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin);

        return new AssignmentDto
        {
            AssignmentId = assignment.Id,
            ComplaintId = assignment.ComplaintId,
            ReferenceNumber = $"CH-{assignment.Complaint.CreatedAt:yyyy}-{assignment.ComplaintId:D6}",
            Title = assignment.Complaint.Title,
            Description = assignment.Complaint.Description,
            Category = assignment.Complaint.Category,
            ComplaintStatus = assignment.Complaint.Status.ToString(),
            Priority = assignment.Complaint.Priority.ToString(),
            DepartmentId = assignment.Complaint.DepartmentId,
            DepartmentName = assignment.Complaint.Department.Name,
            WardId = assignment.Complaint.WardId,
            WardName = assignment.Complaint.Ward.Name,
            Address = assignment.Complaint.Address,
            Latitude = assignment.Complaint.Latitude,
            Longitude = assignment.Complaint.Longitude,
            CitizenId = assignment.Complaint.CitizenId,
            CitizenName = assignment.Complaint.Citizen.FullName,
            OfficerId = assignment.OfficerId,
            OfficerName = assignment.Officer.FullName,
            AssignmentStatus = assignment.Status.ToString(),
            AssignmentReason = assignment.Reason,
            AssignedAt = assignment.AssignedAt,
            AssignmentDueAt = assignment.AssignmentDueAt,
            ResolutionDueAt = assignment.ResolutionDueAt,
            RespondedAt = assignment.RespondedAt,
            CompletedAt = assignment.CompletedAt,
            EstimatedCompletionAt = GetEstimatedCompletion(assignment.Complaint),
            SlaState = slaState,
            RemainingMinutes = remaining,
            CanAccept = officerAction && assignment.IsCurrent && assignment.Status == AssignmentStatus.Pending,
            CanReject = officerAction && assignment.IsCurrent && assignment.Status == AssignmentStatus.Pending,
            CanAddProgress = officerAction && assignment.IsCurrent && assignment.Status == AssignmentStatus.Accepted,
            CanComplete = officerAction && assignment.IsCurrent && assignment.Status == AssignmentStatus.Accepted,
            CanReassign = supervisorAction && assignment.IsCurrent && ReassignableStatuses.Contains(assignment.Complaint.Status),
            CanRequestTransfer = officerAction && assignment.IsCurrent && (assignment.Status == AssignmentStatus.Pending || assignment.Status == AssignmentStatus.Accepted),
            CanRequestCitizenInformation = officerAction && assignment.IsCurrent && assignment.Status == AssignmentStatus.Accepted,
            ProgressUpdates = assignment.Complaint.ProgressUpdates.OrderByDescending(entity => entity.CreatedAt).Select(entity => new ProgressUpdateDto
            {
                Id = entity.Id,
                OfficerName = entity.Officer.FullName,
                Message = entity.Message,
                ProgressPercent = entity.ProgressPercent,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                CreatedAt = entity.CreatedAt
            }).ToArray(),
            ResolutionEvidence = assignment.Complaint.Images.Where(entity => entity.IsResolutionEvidence).OrderByDescending(entity => entity.UploadedAt).Select(entity => new ResolutionEvidenceDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                MimeType = entity.MimeType,
                FileSize = entity.FileSize,
                UploadedAt = entity.UploadedAt,
                DownloadPath = $"/api/v1/complaints/{assignment.ComplaintId}/images/{entity.Id}"
            }).ToArray(),
            ProgressEvidence = assignment.Complaint.Images
                .Where(entity => entity.S3Key.StartsWith($"progress/{assignment.ComplaintId}/"))
                .OrderByDescending(entity => entity.UploadedAt)
                .Select(entity => new ProgressEvidenceDto
                {
                    Id = entity.Id,
                    ProgressUpdateId = GetProgressUpdateId(entity.S3Key, assignment.ComplaintId),
                    FileName = entity.FileName,
                    MimeType = entity.MimeType,
                    FileSize = entity.FileSize,
                    UploadedAt = entity.UploadedAt,
                    DownloadPath = $"/api/v1/complaints/{assignment.ComplaintId}/images/{entity.Id}",
                    EvidenceType = EvidenceType(entity.MimeType)
                }).ToArray()
        };
    }

    private AssignmentDto MapUnassigned(Complaint complaint) => new()
    {
        ComplaintId = complaint.Id,
        ReferenceNumber = $"CH-{complaint.CreatedAt:yyyy}-{complaint.Id:D6}",
        Title = complaint.Title,
        Description = complaint.Description,
        Category = complaint.Category,
        ComplaintStatus = complaint.Status.ToString(),
        Priority = complaint.Priority.ToString(),
        DepartmentId = complaint.DepartmentId,
        DepartmentName = complaint.Department.Name,
        WardId = complaint.WardId,
        WardName = complaint.Ward.Name,
        Address = complaint.Address,
        Latitude = complaint.Latitude,
        Longitude = complaint.Longitude,
        CitizenId = complaint.CitizenId,
        CitizenName = complaint.Citizen.FullName,
        AssignmentStatus = "Unassigned",
        AssignedAt = complaint.CreatedAt,
        AssignmentDueAt = complaint.CreatedAt.Add(GetSla(complaint.Priority).Assignment),
        ResolutionDueAt = complaint.CreatedAt.Add(GetSla(complaint.Priority).Resolution),
        SlaState = complaint.CreatedAt.Add(GetSla(complaint.Priority).Assignment) < DateTimeOffset.UtcNow ? "Breached" : "Unassigned",
        RemainingMinutes = (long)Math.Floor((complaint.CreatedAt.Add(GetSla(complaint.Priority).Assignment) - DateTimeOffset.UtcNow).TotalMinutes),
        CanReassign = true
    };

    private void EnsureCanView(ComplaintAssignment assignment)
    {
        var role = _currentUser.Role ?? string.Empty;
        if (role == nameof(UserRole.Citizen) && assignment.Complaint.CitizenId != RequireUserId())
            throw new UnauthorizedAccessException("You cannot view this assignment.");
        if (role == nameof(UserRole.Officer) && assignment.OfficerId != RequireUserId())
            throw new UnauthorizedAccessException("You cannot view another officer's assignment.");
        if (role == nameof(UserRole.Supervisor)) EnsureSupervisorScope(assignment.Complaint);
    }

    private void EnsureComplaintScope(Complaint complaint)
    {
        var role = _currentUser.Role ?? string.Empty;
        if (role == nameof(UserRole.Citizen) && complaint.CitizenId != RequireUserId())
            throw new UnauthorizedAccessException("You cannot view this complaint assignment history.");
        if (role == nameof(UserRole.Officer) && complaint.AssignedOfficerId != RequireUserId())
            throw new UnauthorizedAccessException("You cannot view this complaint assignment history.");
        if (role == nameof(UserRole.Supervisor)) EnsureSupervisorScope(complaint);
    }

    private IQueryable<Complaint> ApplySupervisorScope(IQueryable<Complaint> source)
    {
        if (_currentUser.Role == nameof(UserRole.Supervisor))
        {
            var departmentId = _currentUser.DepartmentId ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
            source = source.Where(entity => entity.DepartmentId == departmentId);
            if (_currentUser.WardId.HasValue) source = source.Where(entity => entity.WardId == _currentUser.WardId.Value);
        }
        return source;
    }

    private IQueryable<ComplaintAssignment> ApplySupervisorScope(IQueryable<ComplaintAssignment> source)
    {
        if (_currentUser.Role == nameof(UserRole.Supervisor))
        {
            var departmentId = _currentUser.DepartmentId ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
            source = source.Where(entity => entity.Complaint.DepartmentId == departmentId);
            if (_currentUser.WardId.HasValue) source = source.Where(entity => entity.Complaint.WardId == _currentUser.WardId.Value);
        }
        return source;
    }

    private IQueryable<User> ApplyOfficerScope(IQueryable<User> source, long? departmentId, long? wardId)
    {
        if (_currentUser.Role == nameof(UserRole.Supervisor))
        {
            var scopedDepartment = _currentUser.DepartmentId ?? throw new UnauthorizedAccessException("Supervisor department scope is missing.");
            source = source.Where(entity => entity.DepartmentId == scopedDepartment);
            if (_currentUser.WardId.HasValue) source = source.Where(entity => entity.WardId == _currentUser.WardId.Value);
        }
        else
        {
            if (departmentId.HasValue) source = source.Where(entity => entity.DepartmentId == departmentId.Value);
            if (wardId.HasValue) source = source.Where(entity => entity.WardId == wardId.Value);
        }
        return source;
    }

    private void EnsureSupervisorScope(Complaint complaint)
    {
        if (_currentUser.Role != nameof(UserRole.Supervisor)) return;
        if (_currentUser.DepartmentId != complaint.DepartmentId)
            throw new UnauthorizedAccessException("This complaint is outside your department scope.");
        if (_currentUser.WardId.HasValue && _currentUser.WardId != complaint.WardId)
            throw new UnauthorizedAccessException("This complaint is outside your ward scope.");
    }

    private static void ValidateOfficer(User officer, Complaint complaint)
    {
        if (!officer.IsActive || officer.Role != UserRole.Officer)
            throw new BusinessRuleViolationException("The selected user is not an active officer.");
        if (officer.DepartmentId != complaint.DepartmentId)
            throw new BusinessRuleViolationException("The officer must belong to the complaint department.");
        if (officer.WardId != complaint.WardId)
            throw new BusinessRuleViolationException("The officer must belong to the complaint ward.");
    }

    private void EnsureOfficer()
    {
        if (_currentUser.Role != nameof(UserRole.Officer))
            throw new UnauthorizedAccessException("Officer access is required.");
    }

    private void EnsureSupervisorOrAbove()
    {
        if (_currentUser.Role is not (nameof(UserRole.Supervisor) or nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Supervisor or administrator access is required.");
    }

    private long RequireUserId() => _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user ID is missing.");

    private ComplaintTimeline Timeline(string eventType, string description) => new()
    {
        UserId = RequireUserId(),
        EventType = eventType,
        Description = description,
        Timestamp = DateTimeOffset.UtcNow
    };

    private static (TimeSpan Assignment, TimeSpan Resolution) GetSla(ComplaintPriority priority) => priority switch
    {
        ComplaintPriority.Low => (TimeSpan.FromHours(48), TimeSpan.FromDays(7)),
        ComplaintPriority.Medium => (TimeSpan.FromHours(24), TimeSpan.FromDays(5)),
        ComplaintPriority.High => (TimeSpan.FromHours(12), TimeSpan.FromDays(3)),
        ComplaintPriority.Critical => (TimeSpan.FromHours(2), TimeSpan.FromHours(24)),
        _ => (TimeSpan.FromHours(24), TimeSpan.FromDays(5))
    };

    private static DateTimeOffset DueAt(ComplaintAssignment assignment) =>
        assignment.Status == AssignmentStatus.Pending ? assignment.AssignmentDueAt : assignment.ResolutionDueAt;

    private static void ValidateCoordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Latitude and longitude must be supplied together."]);
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Invalid GPS coordinates."]);
    }

    private static void EnsureProgressAllowed(ComplaintAssignment assignment)
    {
        if (assignment.Status != AssignmentStatus.Accepted ||
            assignment.Complaint.Status is not (ComplaintStatus.InProgress or ComplaintStatus.Escalated))
            throw new BusinessRuleViolationException("Accept the assignment before performing this operation.");
    }

    private static void ValidateEstimatedCompletion(DateTimeOffset? value)
    {
        if (!value.HasValue) return;
        var now = DateTimeOffset.UtcNow;
        if (value.Value <= now.AddMinutes(10))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Estimated completion must be at least ten minutes in the future."]);
        if (value.Value > now.AddDays(90))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Estimated completion cannot be more than 90 days in the future."]);
    }

    private static string NormalizeTransferReason(string value)
    {
        var normalized = value.Trim();
        var allowed = new[] { "WrongDepartment", "OutsideScope", "WorkloadCapacity", "SafetyConcern", "SpecialistRequired", "Other" };
        return allowed.FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Select a valid transfer reason."]);
    }

    private static DateTimeOffset? GetEstimatedCompletion(Complaint complaint)
    {
        var value = complaint.Timeline
            .Where(item => item.EventType == "OFFICER_ESTIMATED_COMPLETION")
            .OrderByDescending(item => item.Timestamp)
            .Select(item => item.Description)
            .FirstOrDefault();
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static long GetProgressUpdateId(string objectKey, long complaintId)
    {
        var segments = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 4 && segments[0] == "progress" &&
               long.TryParse(segments[1], out var parsedComplaintId) && parsedComplaintId == complaintId &&
               long.TryParse(segments[2], out var updateId)
            ? updateId
            : 0;
    }

    private static string EvidenceType(string mimeType) => mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        ? "Image"
        : mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "Video" : "Document";

    private static async Task<string> ValidateOfficerEvidenceAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 15L * 1024 * 1024)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Each Officer evidence file must be between 1 byte and 15 MB."]);

        var mime = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        var allowed = mime switch
        {
            "image/jpeg" => new[] { ".jpg", ".jpeg" },
            "image/png" => new[] { ".png" },
            "image/webp" => new[] { ".webp" },
            "video/mp4" => new[] { ".mp4" },
            "video/webm" => new[] { ".webm" },
            "video/quicktime" => new[] { ".mov" },
            "application/pdf" => new[] { ".pdf" },
            "application/msword" => new[] { ".doc" },
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new[] { ".docx" },
            _ => Array.Empty<string>()
        };
        if (allowed.Length == 0)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Officer evidence must be JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC, or DOCX."]);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException([$"The file extension does not match the declared {mime} content type."]);

        await using var stream = file.OpenReadStream();
        var header = new byte[16];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        var valid = mime switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            "video/mp4" or "video/quicktime" => read >= 8 && System.Text.Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            "video/webm" => read >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            "application/pdf" => read >= 4 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "%PDF",
            "application/msword" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            _ => false
        };
        if (!valid)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["The Officer evidence file signature is invalid."]);
        return extension;
    }

    private static string ValidateEvidence(string fileName, string contentType, long length)
    {
        if (length <= 0 || length > 5 * 1024 * 1024)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Each resolution image must be between 1 byte and 5 MB."]);
        var allowed = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };
        if (allowed is null)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Resolution evidence must be JPEG, PNG, or WebP."]);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".jpeg" or ".jpg" or ".png" or ".webp" ? extension : allowed;
    }
}
