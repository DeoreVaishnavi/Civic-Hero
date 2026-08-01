using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Administration;
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

public sealed class AdminComplaintService : IAdminComplaintService
{
    private static readonly HashSet<ComplaintStatus> TerminalStatuses =
    [ComplaintStatus.Closed, ComplaintStatus.ClosedAuto, ComplaintStatus.ClosedFraud, ComplaintStatus.Withdrawn, ComplaintStatus.Merged];

    private static readonly HashSet<ComplaintStatus> ClosedStatuses =
    [ComplaintStatus.Closed, ComplaintStatus.ClosedAuto, ComplaintStatus.ClosedFraud];

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAssignmentService _assignments;
    private readonly INotificationService _notifications;
    private readonly IStorageService _storage;

    public AdminComplaintService(
        CivicDbContext db,
        ICurrentUserService current,
        IAssignmentService assignments,
        INotificationService notifications,
        IStorageService storage)
    {
        _db = db;
        _current = current;
        _assignments = assignments;
        _notifications = notifications;
        _storage = storage;
    }

    public async Task<PagedResponse<AdminComplaintItemResponse>> GetAsync(AdminComplaintQuery query, CancellationToken ct = default)
    {
        EnsureAdmin();
        var source = BaseQuery();
        if (!query.IncludeArchived) source = source.Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(x => x.Title.Contains(term) || x.Description.Contains(term) || x.Address.Contains(term) ||
                x.Citizen.FullName.Contains(term) || x.Citizen.Email.Contains(term));
        }
        if (Enum.TryParse<ComplaintStatus>(query.Status, true, out var status)) source = source.Where(x => x.Status == status);
        if (Enum.TryParse<ComplaintPriority>(query.Priority, true, out var priority)) source = source.Where(x => x.Priority == priority);
        if (!string.IsNullOrWhiteSpace(query.Category)) source = source.Where(x => x.Category == query.Category.Trim());
        if (query.DepartmentId.HasValue) source = source.Where(x => x.DepartmentId == query.DepartmentId.Value);
        if (query.WardId.HasValue) source = source.Where(x => x.WardId == query.WardId.Value);
        if (query.DuplicateOnly) source = source.Where(x => x.DuplicateOfComplaintId.HasValue || x.Status == ComplaintStatus.Merged);

        var total = await source.CountAsync(ct);
        var items = await source.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return new PagedResponse<AdminComplaintItemResponse>
        {
            Items = items.Select(Map).ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<AdminComplaintDetailResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        EnsureAdmin();
        var item = await DetailQuery().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Complaint was not found.");
        return MapDetail(item);
    }

    public async Task<AdminComplaintDetailResponse> ChangePriorityAsync(long id, AdminComplaintPriorityRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        if (!Enum.TryParse<ComplaintPriority>(request.Priority, true, out var priority))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Priority must be Low, Medium, High or Critical."]);
        var complaint = await RequireActiveAsync(id, ct);
        var old = complaint.Priority;
        if (old == priority) throw new BusinessRuleViolationException("The complaint already has the selected priority.");
        complaint.Priority = priority;
        AddTimeline(complaint, "ADMIN_PRIORITY_CHANGED", $"Admin changed priority from {old} to {priority}. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_PRIORITY_CHANGED", complaint.Id, new { priority = old.ToString() }, new { priority = priority.ToString(), reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint priority updated", $"Priority changed from {old} to {priority}.", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> CorrectRoutingAsync(long id, AdminComplaintRoutingRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var department = await _db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.DepartmentId && x.IsActive, ct)
            ?? throw new NotFoundException("Active department was not found.");
        var ward = await _db.Wards.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.WardId && x.DepartmentId == request.DepartmentId && x.IsActive, ct)
            ?? throw new BusinessRuleViolationException("The selected ward is not active in the selected department.");
        var complaint = await RequireActiveAsync(id, ct);
        var old = new { complaint.DepartmentId, complaint.WardId, department = complaint.Department.Name, ward = complaint.Ward.Name };
        if (complaint.DepartmentId == request.DepartmentId && complaint.WardId == request.WardId)
            throw new BusinessRuleViolationException("The complaint already uses the selected routing.");

        var currentAssignment = await _db.ComplaintAssignments.Include(x => x.Officer)
            .SingleOrDefaultAsync(x => x.ComplaintId == id && x.IsCurrent, ct);
        var assignmentInvalidated = currentAssignment is not null &&
            (currentAssignment.Officer.DepartmentId != request.DepartmentId ||
             (currentAssignment.Officer.WardId.HasValue && currentAssignment.Officer.WardId.Value != request.WardId));
        if (assignmentInvalidated)
        {
            currentAssignment!.IsCurrent = false;
            currentAssignment.Status = AssignmentStatus.Cancelled;
            currentAssignment.ReassignedAt = DateTimeOffset.UtcNow;
            currentAssignment.Reason = $"Administrative routing correction: {reason}";
            complaint.AssignedOfficerId = null;
            if (!TerminalStatuses.Contains(complaint.Status)) complaint.Status = ComplaintStatus.ReassignmentPending;
        }

        complaint.DepartmentId = request.DepartmentId;
        complaint.WardId = request.WardId;
        AddTimeline(complaint, "ADMIN_ROUTING_CORRECTED",
            $"Admin routed complaint to {department.Name} / {ward.Name}. Reason: {reason}" +
            (assignmentInvalidated ? " Existing assignment was cancelled because its scope no longer matched." : string.Empty));
        AddAudit("ADMIN_COMPLAINT_ROUTING_CORRECTED", complaint.Id, old,
            new { request.DepartmentId, request.WardId, department = department.Name, ward = ward.Name, reason, assignmentInvalidated });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint routing corrected", $"Your complaint is now routed to {department.Name}, {ward.Name}.", ct);
        if (assignmentInvalidated && currentAssignment is not null)
            await NotifyAsync(currentAssignment.OfficerId, "Assignment removed after routing correction",
                $"Complaint {Reference(complaint)} was moved outside your operational scope.", complaint.Id, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> OverrideAssignmentAsync(long id, AdminComplaintAssignmentRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var complaint = await RequireActiveAsync(id, ct);
        if (TerminalStatuses.Contains(complaint.Status)) throw new BusinessRuleViolationException("A terminal complaint cannot be assigned.");
        var hasCurrent = await _db.ComplaintAssignments.AnyAsync(x => x.ComplaintId == id && x.IsCurrent, ct);
        if (hasCurrent)
            await _assignments.ReassignAsync(id, new ReassignComplaintRequest { OfficerId = request.OfficerId, Reason = reason }, ct);
        else
            await _assignments.AssignAsync(new AssignComplaintRequest { ComplaintId = id, OfficerId = request.OfficerId, Reason = reason }, ct);
        AddAudit("ADMIN_COMPLAINT_ASSIGNMENT_OVERRIDE", id, null, new { request.OfficerId, reason, operation = hasCurrent ? "Reassign" : "Assign" });
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> CloseAsync(long id, AdminComplaintActionRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var complaint = await RequireActiveAsync(id, ct);
        if (ClosedStatuses.Contains(complaint.Status)) throw new BusinessRuleViolationException("The complaint is already closed.");
        if (complaint.Status == ComplaintStatus.Merged) throw new BusinessRuleViolationException("A merged complaint cannot be closed separately.");
        var oldStatus = complaint.Status;
        var assignment = await _db.ComplaintAssignments.SingleOrDefaultAsync(x => x.ComplaintId == id && x.IsCurrent, ct);
        if (assignment is not null)
        {
            assignment.IsCurrent = false;
            assignment.Status = AssignmentStatus.Completed;
            assignment.CompletedAt = DateTimeOffset.UtcNow;
            assignment.Reason = $"Administrative closure: {reason}";
        }
        complaint.Status = ComplaintStatus.Closed;
        complaint.ClosedAt = DateTimeOffset.UtcNow;
        complaint.ResolvedAt ??= DateTimeOffset.UtcNow;
        AddTimeline(complaint, "ADMIN_COMPLAINT_CLOSED", $"Admin closed complaint from {oldStatus}. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_CLOSED", id, new { status = oldStatus.ToString() }, new { status = "Closed", reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint closed by administrator", reason, ct);
        if (complaint.AssignedOfficerId.HasValue)
            await NotifyAsync(complaint.AssignedOfficerId.Value, "Complaint closed by administrator", $"{Reference(complaint)} was closed. {reason}", id, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> ReopenAsync(long id, AdminComplaintActionRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var complaint = await RequireActiveAsync(id, ct);
        if (!ClosedStatuses.Contains(complaint.Status)) throw new BusinessRuleViolationException("Only a closed complaint can be reopened.");
        var oldStatus = complaint.Status;
        complaint.Status = ComplaintStatus.ReassignmentPending;
        complaint.ClosedAt = null;
        complaint.ResolvedAt = null;
        complaint.AssignedOfficerId = null;
        foreach (var assignment in await _db.ComplaintAssignments.Where(x => x.ComplaintId == id && x.IsCurrent).ToListAsync(ct))
        {
            assignment.IsCurrent = false;
            assignment.Status = AssignmentStatus.Cancelled;
            assignment.ReassignedAt = DateTimeOffset.UtcNow;
            assignment.Reason = $"Administrative reopen: {reason}";
        }
        AddTimeline(complaint, "ADMIN_COMPLAINT_REOPENED", $"Admin reopened complaint from {oldStatus} and returned it for reassignment. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_REOPENED", id, new { status = oldStatus.ToString() }, new { status = "ReassignmentPending", reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint reopened", "An administrator reopened your complaint for further work.", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> ArchiveAsync(long id, AdminComplaintActionRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var complaint = await RequireActiveAsync(id, ct);
        if (!TerminalStatuses.Contains(complaint.Status))
            throw new BusinessRuleViolationException("Only closed, withdrawn or merged complaints can be archived.");
        AddTimeline(complaint, "ADMIN_COMPLAINT_ARCHIVED", $"Admin archived the complaint. Reason: {reason}");
        complaint.IsDeleted = true;
        complaint.DeletedAt = DateTimeOffset.UtcNow;
        AddAudit("ADMIN_COMPLAINT_ARCHIVED", id, new { archived = false }, new { archived = true, reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint archived", reason, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> RestoreAsync(long id, AdminComplaintActionRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var complaint = await DetailQuery().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Complaint was not found.");
        if (!complaint.IsDeleted) throw new BusinessRuleViolationException("The complaint is not archived.");
        complaint.IsDeleted = false;
        complaint.DeletedAt = null;
        AddTimeline(complaint, "ADMIN_COMPLAINT_RESTORED", $"Admin restored the archived complaint. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_RESTORED", id, new { archived = true }, new { archived = false, reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint restored", "An administrator restored your archived complaint record.", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> LinkDuplicateAsync(long id, AdminComplaintDuplicateRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var (source, canonical) = await RequireDuplicatePairAsync(id, request.CanonicalComplaintId, ct);
        source.DuplicateOfComplaintId = canonical.Id;
        AddTimeline(source, "ADMIN_DUPLICATE_LINKED", $"Admin linked this complaint to canonical complaint {Reference(canonical)}. Reason: {reason}");
        AddTimeline(canonical, "ADMIN_RELATED_DUPLICATE_LINKED", $"Admin linked complaint {Reference(source)} as a related duplicate. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_DUPLICATE_LINKED", source.Id, null, new { canonicalComplaintId = canonical.Id, reason });
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> MergeAsync(long id, AdminComplaintDuplicateRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var (source, canonical) = await RequireDuplicatePairAsync(id, request.CanonicalComplaintId, ct);
        if (source.Status == ComplaintStatus.Merged) throw new BusinessRuleViolationException("The complaint is already merged.");

        var canonicalVoters = await _db.ComplaintVotes.Where(x => x.ComplaintId == canonical.Id).Select(x => x.UserId).ToHashSetAsync(ct);
        var sourceVotes = await _db.ComplaintVotes.Where(x => x.ComplaintId == source.Id).ToListAsync(ct);
        foreach (var vote in sourceVotes)
        {
            if (!canonicalVoters.Contains(vote.UserId))
            {
                canonical.Votes.Add(new ComplaintVote { UserId = vote.UserId, VotedAt = vote.VotedAt });
                canonicalVoters.Add(vote.UserId);
            }
            _db.ComplaintVotes.Remove(vote);
        }

        foreach (var assignment in await _db.ComplaintAssignments.Where(x => x.ComplaintId == source.Id && x.IsCurrent).ToListAsync(ct))
        {
            assignment.IsCurrent = false;
            assignment.Status = AssignmentStatus.Cancelled;
            assignment.ReassignedAt = DateTimeOffset.UtcNow;
            assignment.Reason = $"Complaint merged into {Reference(canonical)}: {reason}";
        }
        source.AssignedOfficerId = null;
        source.DuplicateOfComplaintId = canonical.Id;
        source.Status = ComplaintStatus.Merged;
        source.ClosedAt = DateTimeOffset.UtcNow;
        AddTimeline(source, "ADMIN_COMPLAINT_MERGED", $"Admin merged this complaint into {Reference(canonical)}. Reason: {reason}");
        AddTimeline(canonical, "ADMIN_DUPLICATE_MERGED", $"Admin merged {Reference(source)} into this complaint. Unique community supports were transferred. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_MERGED", source.Id, null, new { canonicalComplaintId = canonical.Id, transferredVotes = sourceVotes.Count, reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(source, "Complaint merged with an existing issue", $"Your complaint was merged into {Reference(canonical)}. {reason}", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<AdminComplaintDetailResponse> RemoveMediaAsync(long id, long mediaId, AdminComplaintActionRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();
        var reason = ValidReason(request.Reason);
        var complaint = await DetailQuery().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Complaint was not found.");
        var media = complaint.Images.SingleOrDefault(x => x.Id == mediaId)
            ?? throw new NotFoundException("Complaint media was not found.");
        await _storage.DeleteAsync(media.S3Key, ct);
        _db.ComplaintImages.Remove(media);
        AddTimeline(complaint, "ADMIN_UNSAFE_MEDIA_REMOVED", $"Admin removed unsafe or non-compliant media '{media.FileName}'. Reason: {reason}");
        AddAudit("ADMIN_COMPLAINT_MEDIA_REMOVED", complaint.Id,
            new { media.Id, media.FileName, media.MimeType, media.FileSize, media.IsResolutionEvidence }, new { reason });
        await _db.SaveChangesAsync(ct);
        await NotifyCitizenAsync(complaint, "Complaint media removed", $"An administrator removed {media.FileName}. {reason}", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<DuplicateClusterResponse>> GetDuplicateClustersAsync(bool includeArchived, CancellationToken ct = default)
    {
        EnsureAdmin();
        var source = BaseQuery();
        if (!includeArchived) source = source.Where(x => !x.IsDeleted);
        var duplicates = await source.Where(x => x.DuplicateOfComplaintId.HasValue).OrderByDescending(x => x.UpdatedAt).ToListAsync(ct);
        if (duplicates.Count == 0) return [];
        var canonicalIds = duplicates.Select(x => x.DuplicateOfComplaintId!.Value).Distinct().ToArray();
        var canonicals = await BaseQuery().Where(x => canonicalIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return duplicates.GroupBy(x => x.DuplicateOfComplaintId!.Value)
            .Where(group => canonicals.ContainsKey(group.Key))
            .Select(group => new DuplicateClusterResponse
            {
                Canonical = Map(canonicals[group.Key]),
                Duplicates = group.Select(Map).ToArray(),
                TotalSupportCount = canonicals[group.Key].Votes.Count + group.Sum(x => x.Votes.Count)
            })
            .OrderByDescending(x => x.Duplicates.Count)
            .ToArray();
    }

    private IQueryable<Complaint> BaseQuery() => _db.Complaints.IgnoreQueryFilters().AsNoTracking()
        .Include(x => x.Citizen).Include(x => x.AssignedOfficer).Include(x => x.Department).Include(x => x.Ward)
        .Include(x => x.Images).Include(x => x.Votes);

    private IQueryable<Complaint> DetailQuery() => _db.Complaints.IgnoreQueryFilters()
        .Include(x => x.Citizen).Include(x => x.AssignedOfficer).Include(x => x.Department).Include(x => x.Ward)
        .Include(x => x.Images).Include(x => x.Votes)
        .Include(x => x.Timeline).ThenInclude(x => x.User);

    private async Task<Complaint> RequireActiveAsync(long id, CancellationToken ct)
    {
        var complaint = await DetailQuery().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Complaint was not found.");
        if (complaint.IsDeleted) throw new BusinessRuleViolationException("Restore the archived complaint before changing it.");
        return complaint;
    }

    private async Task<(Complaint Source, Complaint Canonical)> RequireDuplicatePairAsync(long sourceId, long canonicalId, CancellationToken ct)
    {
        if (sourceId == canonicalId) throw new CivicHero.Backend.Core.Exceptions.ValidationException(["A complaint cannot be linked or merged into itself."]);
        var items = await DetailQuery().Where(x => x.Id == sourceId || x.Id == canonicalId).ToListAsync(ct);
        var source = items.SingleOrDefault(x => x.Id == sourceId) ?? throw new NotFoundException("Source complaint was not found.");
        var canonical = items.SingleOrDefault(x => x.Id == canonicalId) ?? throw new NotFoundException("Canonical complaint was not found.");
        if (source.IsDeleted || canonical.IsDeleted) throw new BusinessRuleViolationException("Archived complaints must be restored before duplicate management.");
        if (canonical.Status is ComplaintStatus.Merged or ComplaintStatus.Withdrawn or ComplaintStatus.ClosedFraud)
            throw new BusinessRuleViolationException("The selected canonical complaint cannot receive duplicate links.");
        return (source, canonical);
    }

    private AdminComplaintDetailResponse MapDetail(Complaint item) => new()
    {
        Complaint = Map(item),
        Media = item.Images.OrderByDescending(x => x.UploadedAt).Select(x => new AdminComplaintMediaResponse
        {
            Id = x.Id, FileName = x.FileName, MimeType = x.MimeType, FileSize = x.FileSize,
            IsResolutionEvidence = x.IsResolutionEvidence, UploadedAt = x.UploadedAt,
            DownloadPath = $"/api/v1/complaints/{item.Id}/images/{x.Id}"
        }).ToArray(),
        Timeline = item.Timeline.OrderByDescending(x => x.Timestamp).Select(x => new AdminComplaintTimelineResponse
        {
            Id = x.Id, EventType = x.EventType, Description = x.Description,
            ActorName = x.User?.FullName ?? "System", Timestamp = x.Timestamp
        }).ToArray()
    };

    private static AdminComplaintItemResponse Map(Complaint x) => new()
    {
        Id = x.Id,
        ReferenceNumber = Reference(x),
        Title = x.Title,
        Description = x.Description,
        Status = x.Status.ToString(),
        Priority = x.Priority.ToString(),
        Category = x.Category,
        DepartmentId = x.DepartmentId,
        DepartmentName = x.Department.Name,
        WardId = x.WardId,
        WardName = x.Ward.Name,
        CitizenId = x.CitizenId,
        CitizenName = x.IsAnonymous ? "Anonymous citizen" : x.Citizen.FullName,
        CitizenEmail = x.IsAnonymous ? string.Empty : x.Citizen.Email,
        AssignedOfficerId = x.AssignedOfficerId,
        AssignedOfficerName = x.AssignedOfficer?.FullName,
        DuplicateOfComplaintId = x.DuplicateOfComplaintId,
        MediaCount = x.Images.Count,
        IsArchived = x.IsDeleted,
        ArchivedAt = x.DeletedAt,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        CanClose = !x.IsDeleted && !ClosedStatuses.Contains(x.Status) && x.Status != ComplaintStatus.Merged,
        CanReopen = !x.IsDeleted && ClosedStatuses.Contains(x.Status),
        CanArchive = !x.IsDeleted && TerminalStatuses.Contains(x.Status),
        CanRestore = x.IsDeleted,
        CanAssign = !x.IsDeleted && !TerminalStatuses.Contains(x.Status),
        CanMerge = !x.IsDeleted && x.Status != ComplaintStatus.Merged
    };

    private void AddTimeline(Complaint complaint, string eventType, string description) => complaint.Timeline.Add(new ComplaintTimeline
    {
        UserId = RequireUser(), EventType = eventType, Description = description, Timestamp = DateTimeOffset.UtcNow
    });

    private void AddAudit(string action, long complaintId, object? oldValues, object? newValues) => _db.AuditLogs.Add(new AuditLog
    {
        UserId = RequireUser(), UserEmail = _current.Email, UserRole = _current.Role,
        Action = action, EntityName = "Complaint", EntityId = complaintId.ToString(),
        OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
        NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
        Severity = "Warning", Success = true, HttpStatusCode = 200, CreatedAt = DateTimeOffset.UtcNow
    });

    private async Task NotifyCitizenAsync(Complaint complaint, string title, string message, CancellationToken ct) =>
        await NotifyAsync(complaint.CitizenId, title, message, complaint.Id, ct);

    private async Task NotifyAsync(long userId, string title, string message, long complaintId, CancellationToken ct) =>
        await _notifications.SendAsync(new NotificationDispatchRequest(userId, title, message,
            nameof(NotificationType.ComplaintProgress), "Complaint", complaintId, $"/citizen/complaints/{complaintId}"), ct);

    private string ValidReason(string? value)
    {
        var reason = (value ?? string.Empty).Trim();
        if (reason.Length is < 10 or > 1000)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Reason must contain 10 to 1000 characters."]);
        return reason;
    }

    private void EnsureAdmin()
    {
        if (_current.Role is not (nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)))
            throw new UnauthorizedAccessException("Admin or SuperAdmin access is required.");
    }

    private long RequireUser() => _current.UserId ?? throw new UnauthorizedAccessException("Authenticated user is required.");
    private static string Reference(Complaint complaint) => $"CH-{complaint.CreatedAt:yyyy}-{complaint.Id:D6}";
}
