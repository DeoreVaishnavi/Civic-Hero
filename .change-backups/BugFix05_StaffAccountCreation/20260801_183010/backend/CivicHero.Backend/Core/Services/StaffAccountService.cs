using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Staff;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Validation;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class StaffAccountService : IStaffAccountService
{
    private const string EntityName = "StaffAccount";
    private const string PendingAction = "StaffAccountCreatedPendingApproval";
    private const string CreatedApprovedAction = "StaffAccountCreatedApproved";
    private const string ApprovedAction = "StaffAccountApproved";
    private const string RejectedAction = "StaffAccountRejected";

    private readonly CivicDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public StaffAccountService(
        CivicDbContext db,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<StaffAccountDto> CreateAsync(
        CreateStaffAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = RequireActorId();
        var actorRole = RequireAdministratorRole();
        var role = ParseStaffRole(request.Role);
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(user => user.Email == email, cancellationToken);
        if (emailExists)
            throw new ConflictException("An account with this email already exists.");

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = PhoneNumberNormalizer.Normalize(request.Phone);
            var phoneExists = await _db.Users.IgnoreQueryFilters()
                .AnyAsync(user => user.NormalizedPhone == phone, cancellationToken);
            if (phoneExists)
                throw new ConflictException("An account with this phone number already exists.");
        }

        var department = await _db.Departments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Selected department was not found.");
        if (!department.IsActive)
            throw new BusinessRuleViolationException("Selected department is inactive.");

        Ward? ward = null;
        if (request.WardId.HasValue)
        {
            ward = await _db.Wards.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == request.WardId.Value, cancellationToken)
                ?? throw new NotFoundException("Selected ward was not found.");
            if (!ward.IsActive || ward.DepartmentId != department.Id)
                throw new BusinessRuleViolationException("Selected ward must be active and belong to the selected department.");
        }

        if (role == UserRole.Officer && ward is null)
            throw new BusinessRuleViolationException("Officer accounts require a ward.");

        var isImmediatelyApproved = actorRole == UserRole.SuperAdmin;
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            FullName = PersonNameRules.Normalize(request.FullName),
            Email = email,
            Phone = phone,
            NormalizedPhone = phone,
            IsPhoneVerified = false,
            PasswordHash = _passwordHasher.Hash(request.TemporaryPassword),
            Role = role,
            DepartmentId = department.Id,
            WardId = ward?.Id,
            IsEmailVerified = true,
            IsActive = isImmediatelyApproved,
            AuthorizationVersion = 1
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _db.AuditLogs.Add(CreateWorkflowAudit(
            isImmediatelyApproved ? CreatedApprovedAction : PendingAction,
            user.Id,
            actorId,
            new
            {
                userId = user.Id,
                user.FullName,
                user.Email,
                role = user.Role.ToString(),
                user.DepartmentId,
                user.WardId,
                approvalStatus = isImmediatelyApproved ? "Approved" : "PendingApproval"
            },
            now));

        if (!isImmediatelyApproved)
        {
            var superAdminIds = await _db.Users.AsNoTracking()
                .Where(item => item.Role == UserRole.SuperAdmin && item.IsActive)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);

            foreach (var superAdminId in superAdminIds)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = superAdminId,
                    Title = "Staff account approval required",
                    Message = $"{user.FullName} was created as {user.Role} and is waiting for verification.",
                    Type = NotificationType.SecurityAlert,
                    ReferenceType = EntityName,
                    ReferenceId = user.Id,
                    ActionUrl = "/admin/staff-accounts",
                    CreatedAt = now
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<StaffAccountDto>> GetAsync(
        StaffAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        RequireAdministratorRole();

        var users = await _db.Users.AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Ward)
            .Where(item => item.Role == UserRole.Officer || item.Role == UserRole.Supervisor)
            .OrderByDescending(item => item.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        var ids = users.Select(item => item.Id.ToString()).ToList();
        List<AuditLog> logs;
        if (ids.Count == 0)
        {
            logs = [];
        }
        else
        {
            logs = await _db.AuditLogs.AsNoTracking()
                .Where(item => item.EntityName == EntityName && item.EntityId != null && ids.Contains(item.EntityId))
                .OrderBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        var logLookup = logs.GroupBy(item => item.EntityId ?? string.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        IEnumerable<StaffAccountDto> result = users.Select(user =>
            Map(user, logLookup.GetValueOrDefault(user.Id.ToString()) ?? []));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            result = result.Where(item =>
                item.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Role.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (item.DepartmentName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.WardName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            result = result.Where(item => string.Equals(item.ApprovalStatus, query.Status.Trim(), StringComparison.OrdinalIgnoreCase));

        return result.ToList();
    }

    public async Task<StaffAccountDto> ReviewAsync(
        long userId,
        ReviewStaffAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = RequireActorId();
        var actorRole = RequireAdministratorRole();
        if (actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can verify staff accounts.");

        var user = await _db.Users
            .Include(item => item.Department)
            .Include(item => item.Ward)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Staff account was not found.");

        if (user.Role is not UserRole.Officer and not UserRole.Supervisor)
            throw new BusinessRuleViolationException("Only Officer and Supervisor accounts can be reviewed.");

        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(item => item.EntityName == EntityName && item.EntityId == userId.ToString())
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var pending = logs.LastOrDefault(item => item.Action == PendingAction);
        var previousReview = logs.LastOrDefault(item => item.Action is ApprovedAction or RejectedAction);
        if (pending is null || (previousReview is not null && previousReview.CreatedAt >= pending.CreatedAt))
            throw new BusinessRuleViolationException("This staff account is not waiting for approval.");

        var approve = string.Equals(request.Decision, "Approve", StringComparison.OrdinalIgnoreCase);
        user.IsActive = approve;
        user.AuthorizationVersion++;
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;

        var now = DateTimeOffset.UtcNow;
        _db.AuditLogs.Add(CreateWorkflowAudit(
            approve ? ApprovedAction : RejectedAction,
            user.Id,
            actorId,
            new
            {
                userId = user.Id,
                decision = approve ? "Approve" : "Reject",
                remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
                isActive = user.IsActive
            },
            now));

        if (approve)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Staff account approved",
                Message = "Your CivicHero staff account has been verified by a SuperAdmin. You can now sign in.",
                Type = NotificationType.SecurityAlert,
                ReferenceType = EntityName,
                ReferenceId = user.Id,
                ActionUrl = "/login",
                CreatedAt = now
            });
        }

        if (pending.UserId.HasValue && pending.UserId.Value != actorId)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = pending.UserId.Value,
                Title = approve ? "Staff account approved" : "Staff account rejected",
                Message = $"{user.FullName}'s {user.Role} account was {(approve ? "approved" : "rejected")} by SuperAdmin.",
                Type = NotificationType.SecurityAlert,
                ReferenceType = EntityName,
                ReferenceId = user.Id,
                ActionUrl = "/admin/staff-accounts",
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(user.Id, cancellationToken);
    }

    private async Task<StaffAccountDto> GetByIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Ward)
            .SingleAsync(item => item.Id == userId, cancellationToken);
        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(item => item.EntityName == EntityName && item.EntityId == userId.ToString())
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return Map(user, logs);
    }

    private AuditLog CreateWorkflowAudit(string action, long targetUserId, long actorId, object values, DateTimeOffset createdAt) =>
        new()
        {
            UserId = actorId,
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = action,
            EntityName = EntityName,
            EntityId = targetUserId.ToString(),
            NewValuesJson = JsonSerializer.Serialize(values),
            Severity = "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = createdAt
        };

    private static StaffAccountDto Map(User user, IReadOnlyList<AuditLog> logs)
    {
        var creation = logs.LastOrDefault(item => item.Action is PendingAction or CreatedApprovedAction);
        var review = logs.LastOrDefault(item => item.Action is ApprovedAction or RejectedAction);

        var status = review?.Action switch
        {
            ApprovedAction => "Approved",
            RejectedAction => "Rejected",
            _ when creation?.Action == PendingAction => "PendingApproval",
            _ when creation?.Action == CreatedApprovedAction => "Approved",
            _ when user.IsActive => "ExistingActive",
            _ => "Inactive"
        };

        return new StaffAccountDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            WardId = user.WardId,
            WardName = user.Ward?.Name,
            IsActive = user.IsActive,
            ApprovalStatus = status,
            RequestedBy = creation?.UserEmail,
            RequestedAt = creation?.CreatedAt,
            ReviewedBy = review?.UserEmail,
            ReviewedAt = review?.CreatedAt,
            ReviewRemarks = ReadRemarks(review?.NewValuesJson),
            CreatedAt = user.CreatedAt
        };
    }

    private static string? ReadRemarks(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("remarks", out var remarks)
                && remarks.ValueKind == JsonValueKind.String
                ? remarks.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private long RequireActorId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated administrator identifier is missing.");

    private UserRole RequireAdministratorRole()
    {
        if (!Enum.TryParse<UserRole>(_currentUser.Role, true, out var role)
            || role is not UserRole.Admin and not UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Administrator permission is required.");
        return role;
    }

    private static UserRole ParseStaffRole(string role)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsed)
            || parsed is not UserRole.Officer and not UserRole.Supervisor)
            throw new BusinessRuleViolationException("Only Officer or Supervisor accounts can be created here.");
        return parsed;
    }
}
