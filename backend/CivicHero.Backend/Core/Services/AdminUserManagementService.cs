using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Validation;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class AdminUserManagementService : IAdminUserManagementService
{
    private const string DepartmentHeadsKey = "Administration.DepartmentHeads";
    private const string MasterDataKey = "Administration.MasterDataConfiguration";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static readonly Regex FeatureKeyPattern = new("^[A-Za-z][A-Za-z0-9._-]{2,79}$", RegexOptions.Compiled);
    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHostEnvironment _environment;

    public AdminUserManagementService(
        CivicDbContext db,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        IHostEnvironment environment)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    public async Task<AdminPagedResult<AdminManagedUserDto>> GetUsersAsync(
        AdminUserManagementQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Include(x => x.Department).Include(x => x.Ward)
            .Where(x => !x.IsSystemAccount);

        if (!query.IncludeDeleted) source = source.Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.FullName.Contains(search) || x.Email.Contains(search) ||
                (x.Phone != null && x.Phone.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<UserRole>(query.Role, true, out var role))
            source = source.Where(x => x.Role == role);
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive.Value);
        if (query.DepartmentId.HasValue) source = source.Where(x => x.DepartmentId == query.DepartmentId.Value);
        if (query.WardId.HasValue) source = source.Where(x => x.WardId == query.WardId.Value);

        var total = await source.CountAsync(cancellationToken);
        var users = await source.OrderBy(x => x.IsDeleted).ThenByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new AdminPagedResult<AdminManagedUserDto>(users.Select(Map).ToArray(), query.Page, query.PageSize, total);
    }

    public async Task<AdminCreateCitizenResponse> CreateCitizenAsync(
        AdminCreateCitizenRequest request,
        CancellationToken cancellationToken = default)
    {
        var fullName = PersonNameRules.Normalize(request.FullName);
        if (!PersonNameRules.IsValid(fullName))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["A valid full name is required."]);
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["A valid email address is required."]);
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == email, cancellationToken))
            throw new ConflictException("An active or archived account already uses this email address.");

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = PhoneNumberNormalizer.Normalize(request.Phone);
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(x => x.NormalizedPhone == phone, cancellationToken))
                throw new ConflictException("An active or archived account already uses this phone number.");
        }

        var password = string.IsNullOrWhiteSpace(request.TemporaryPassword)
            ? GenerateTemporaryPassword()
            : request.TemporaryPassword;
        ValidatePassword(password);
        string? verificationToken = null;
        if (!request.MarkEmailVerified) verificationToken = GenerateSecureToken();

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            NormalizedPhone = phone,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Citizen,
            IsActive = true,
            IsEmailVerified = request.MarkEmailVerified,
            IsPhoneVerified = false,
            EmailVerificationTokenHash = verificationToken is null ? null : HashToken(verificationToken),
            EmailVerificationTokenExpiresAt = verificationToken is null ? null : DateTimeOffset.UtcNow.AddHours(24)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        AddAudit("ADMIN_CITIZEN_CREATED", user.Id, null, new
        {
            user.Email, user.FullName, user.Role, user.IsActive, user.IsEmailVerified
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminCreateCitizenResponse(Map(user), password, !user.IsEmailVerified,
            user.EmailVerificationTokenExpiresAt, _environment.IsDevelopment() ? verificationToken : null);
    }

    public async Task<AdminUpdateUserEmailResponse> UpdateEmailAsync(
        long userId,
        AdminUpdateUserEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);
        var target = await GetManageableUserAsync(userId, cancellationToken);
        EnsureNotDeleted(target);
        var newEmail = NormalizeEmail(request.NewEmail);
        if (!IsValidEmail(newEmail))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["A valid email address is required."]);
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == newEmail && x.Id != target.Id, cancellationToken))
            throw new ConflictException("Another active or archived account already uses this email address.");
        if (string.Equals(target.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("The new email address is the same as the current email address.");

        var old = new { target.Email, target.IsEmailVerified };
        string? verificationToken = null;
        target.Email = newEmail;
        target.IsEmailVerified = request.MarkVerified;
        if (request.MarkVerified)
        {
            target.EmailVerificationTokenHash = null;
            target.EmailVerificationTokenExpiresAt = null;
        }
        else
        {
            verificationToken = GenerateSecureToken();
            target.EmailVerificationTokenHash = HashToken(verificationToken);
            target.EmailVerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        }
        InvalidateSessions(target);
        AddAudit("ADMIN_USER_EMAIL_CHANGED", target.Id, old, new
        {
            target.Email, target.IsEmailVerified, request.Reason
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new AdminUpdateUserEmailResponse(Map(target), !target.IsEmailVerified,
            target.EmailVerificationTokenExpiresAt, _environment.IsDevelopment() ? verificationToken : null);
    }

    public async Task<AdminManagedUserDto> SoftDeleteAsync(
        long userId,
        AdminUserLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);
        var target = await GetManageableUserAsync(userId, cancellationToken);
        if (target.IsDeleted) throw new BusinessRuleViolationException("This account is already soft-deleted.");
        if (target.Id == RequireActorId()) throw new BusinessRuleViolationException("You cannot delete your own account.");
        if (target.IsSystemAccount) throw new BusinessRuleViolationException("System accounts cannot be deleted.");
        var old = new { target.IsActive, target.IsDeleted, target.DeletedAt };
        target.IsDeleted = true;
        target.DeletedAt = DateTimeOffset.UtcNow;
        target.IsActive = false;
        InvalidateSessions(target);
        AddAudit("ADMIN_USER_SOFT_DELETED", target.Id, old, new
        {
            target.IsActive, target.IsDeleted, target.DeletedAt, request.Reason
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Map(target);
    }

    public async Task<AdminManagedUserDto> RestoreAsync(
        long userId,
        AdminUserLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);
        var target = await GetManageableUserAsync(userId, cancellationToken);
        if (!target.IsDeleted) throw new BusinessRuleViolationException("This account is not soft-deleted.");
        var old = new { target.IsActive, target.IsDeleted, target.DeletedAt };
        target.IsDeleted = false;
        target.DeletedAt = null;
        target.IsActive = request.ActivateOnRestore;
        InvalidateSessions(target);
        AddAudit("ADMIN_USER_RESTORED", target.Id, old, new
        {
            target.IsActive, target.IsDeleted, target.DeletedAt, request.Reason
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Map(target);
    }

    public async Task<AdminUserHistoryResponse> GetHistoryAsync(
        long userId,
        int page,
        int pageSize,
        bool roleChangesOnly,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var target = await GetViewableUserAsync(userId, cancellationToken);
        var id = userId.ToString();
        var source = _db.AuditLogs.AsNoTracking().Where(x =>
            (x.EntityName == "User" && x.EntityId == id) ||
            (x.EntityName == "Users" && x.EntityId == id) ||
            x.UserId == userId);
        if (roleChangesOnly)
        {
            source = source.Where(x => x.Action == "USER_ROLE_CHANGED" ||
                (x.EntityName == "Users" && x.Action.Contains("ChangeRole")));
        }
        var total = await source.CountAsync(cancellationToken);
        var rows = await source.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = rows.Select(x => new AdminUserHistoryItemDto(
            x.Id, x.Action, Category(x.Action), x.UserId, x.UserEmail, x.UserRole,
            x.OldValuesJson, x.NewValuesJson, x.Success, x.HttpStatusCode, x.CreatedAt)).ToArray();
        return new AdminUserHistoryResponse(Map(target), items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<DepartmentHeadDto>> GetDepartmentHeadsAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await ReadDepartmentHeadsAsync(cancellationToken);
        var departments = await _db.Departments.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var userIds = assignments.Select(x => x.UserId).Distinct().ToArray();
        var users = await _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(x => userIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        return departments.Select(department =>
        {
            var assignment = assignments.FirstOrDefault(x => x.DepartmentId == department.Id);
            users.TryGetValue(assignment?.UserId ?? 0, out var user);
            return new DepartmentHeadDto(department.Id, department.Name, department.Code,
                user?.Id, user?.FullName, user?.Email, user?.Role.ToString(),
                assignment?.AssignedAt, assignment?.AssignedByUserId);
        }).ToArray();
    }

    public async Task<DepartmentHeadDto> AssignDepartmentHeadAsync(
        long departmentId,
        DepartmentHeadAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);
        var department = await _db.Departments.FirstOrDefaultAsync(x => x.Id == departmentId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Department was not found or is inactive.");
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException("Selected user account was not found.");
        if (user.IsDeleted || !user.IsActive || user.Role != UserRole.Supervisor || user.DepartmentId != departmentId)
            throw new BusinessRuleViolationException("Department head must be an active Supervisor assigned to this department.");

        var assignments = (await ReadDepartmentHeadsAsync(cancellationToken)).ToList();
        var previous = assignments.FirstOrDefault(x => x.DepartmentId == departmentId);
        assignments.RemoveAll(x => x.DepartmentId == departmentId || x.UserId == user.Id);
        var assignment = new DepartmentHeadAssignmentState
        {
            DepartmentId = departmentId,
            UserId = user.Id,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedByUserId = RequireActorId()
        };
        assignments.Add(assignment);
        await WriteDepartmentHeadsAsync(assignments, cancellationToken);
        AddAudit("DEPARTMENT_HEAD_ASSIGNED", departmentId, previous, new
        {
            departmentId, userId = user.Id, user.FullName, request.Reason
        }, "Department");
        await _db.SaveChangesAsync(cancellationToken);
        return new DepartmentHeadDto(department.Id, department.Name, department.Code,
            user.Id, user.FullName, user.Email, user.Role.ToString(), assignment.AssignedAt, assignment.AssignedByUserId);
    }

    public async Task RemoveDepartmentHeadAsync(
        long departmentId,
        DepartmentHeadRemovalRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateReason(request.Reason);
        var assignments = (await ReadDepartmentHeadsAsync(cancellationToken)).ToList();
        var previous = assignments.FirstOrDefault(x => x.DepartmentId == departmentId)
            ?? throw new NotFoundException("This department does not have an assigned head.");
        assignments.Remove(previous);
        await WriteDepartmentHeadsAsync(assignments, cancellationToken);
        AddAudit("DEPARTMENT_HEAD_REMOVED", departmentId, previous, new { departmentId, request.Reason }, "Department");
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminMasterDataConfigurationDto> GetMasterDataAsync(CancellationToken cancellationToken = default)
    {
        var setting = await GetOrCreateSettingAsync(MasterDataKey, BuildDefaultMasterData(), "MasterData", cancellationToken);
        var result = DeserializeOrDefault<AdminMasterDataConfigurationDto>(setting.Value, BuildDefaultMasterData());
        result.UpdatedAt = setting.UpdatedAt;
        result.UpdatedByUserId = setting.UpdatedByUserId;
        return result;
    }

    public async Task<AdminMasterDataConfigurationDto> UpdateMasterDataAsync(
        AdminMasterDataConfigurationDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        ValidateMasterData(request);
        var setting = await GetOrCreateSettingAsync(MasterDataKey, BuildDefaultMasterData(), "MasterData", cancellationToken);
        var oldValue = setting.Value;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedByUserId = RequireActorId();
        setting.Value = JsonSerializer.Serialize(request, JsonOptions);
        setting.UpdatedByUserId = request.UpdatedByUserId;
        AddAudit("ADMIN_MASTER_DATA_UPDATED", 0, oldValue, new
        {
            priorities = request.Priorities.Count,
            statuses = request.Statuses.Count,
            featureFlags = request.FeatureFlags.Count,
            authorities = request.Authorities.Count
        }, "SystemSetting");
        await _db.SaveChangesAsync(cancellationToken);
        return request;
    }

    private async Task<User> GetManageableUserAsync(long userId, CancellationToken cancellationToken)
    {
        var target = await _db.Users.IgnoreQueryFilters().Include(x => x.Department).Include(x => x.Ward)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User account was not found.");
        var actorRole = RequireActorRole();
        if (target.Role == UserRole.SuperAdmin && actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can manage a SuperAdmin account.");
        if (target.Role == UserRole.Admin && actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can manage an Admin account.");
        return target;
    }

    private async Task<User> GetViewableUserAsync(long userId, CancellationToken cancellationToken)
    {
        var target = await _db.Users.IgnoreQueryFilters().AsNoTracking().Include(x => x.Department).Include(x => x.Ward)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User account was not found.");
        if (target.Role == UserRole.SuperAdmin && RequireActorRole() != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can view SuperAdmin account history.");
        return target;
    }

    private async Task<IReadOnlyList<DepartmentHeadAssignmentState>> ReadDepartmentHeadsAsync(CancellationToken cancellationToken)
    {
        var setting = await GetOrCreateSettingAsync(DepartmentHeadsKey,
            Array.Empty<DepartmentHeadAssignmentState>(), "Administration", cancellationToken);
        return DeserializeOrDefault<IReadOnlyList<DepartmentHeadAssignmentState>>(
            setting.Value, Array.Empty<DepartmentHeadAssignmentState>());
    }

    private async Task WriteDepartmentHeadsAsync(
        IReadOnlyList<DepartmentHeadAssignmentState> assignments,
        CancellationToken cancellationToken)
    {
        var setting = await GetOrCreateSettingAsync(DepartmentHeadsKey,
            Array.Empty<DepartmentHeadAssignmentState>(), "Administration", cancellationToken);
        setting.Value = JsonSerializer.Serialize(assignments, JsonOptions);
        setting.UpdatedByUserId = RequireActorId();
    }

    private async Task<SystemSetting> GetOrCreateSettingAsync<T>(
        string key,
        T defaultValue,
        string group,
        CancellationToken cancellationToken)
    {
        var existing = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (existing is not null) return existing;
        var setting = new SystemSetting
        {
            Key = key,
            Value = JsonSerializer.Serialize(defaultValue, JsonOptions),
            ValueType = "Json",
            Description = key == DepartmentHeadsKey
                ? "Department-to-Supervisor head assignments."
                : "Operational priorities, statuses, SLA, verification, escalation, feature flags and authorities.",
            Group = group,
            IsPublic = false,
            IsSensitive = false,
            UpdatedByUserId = _currentUser.UserId
        };
        _db.SystemSettings.Add(setting);
        await _db.SaveChangesAsync(cancellationToken);
        return setting;
    }

    private static T DeserializeOrDefault<T>(string json, T fallback)
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback; }
        catch (JsonException) { return fallback; }
    }

    private static AdminMasterDataConfigurationDto BuildDefaultMasterData() => new()
    {
        Priorities = new[]
        {
            new PriorityRuleDto { Name = "Low", DisplayName = "Low", IsActive = true, SortOrder = 10, AssignmentHours = 48, ResolutionHours = 168 },
            new PriorityRuleDto { Name = "Medium", DisplayName = "Medium", IsActive = true, SortOrder = 20, AssignmentHours = 24, ResolutionHours = 120 },
            new PriorityRuleDto { Name = "High", DisplayName = "High", IsActive = true, SortOrder = 30, AssignmentHours = 12, ResolutionHours = 72 },
            new PriorityRuleDto { Name = "Critical", DisplayName = "Critical", IsActive = true, SortOrder = 40, AssignmentHours = 2, ResolutionHours = 24 }
        },
        Statuses = Enum.GetValues<ComplaintStatus>().Select((status, index) => new StatusRuleDto
        {
            Name = status.ToString(),
            DisplayName = SplitWords(status.ToString()),
            IsActive = true,
            CitizenVisible = status is not ComplaintStatus.AiTriage and not ComplaintStatus.FraudReview,
            IsTerminal = status is ComplaintStatus.Closed or ComplaintStatus.ClosedAuto or ComplaintStatus.ClosedFraud or ComplaintStatus.Withdrawn or ComplaintStatus.Merged,
            SortOrder = (index + 1) * 10
        }).ToArray(),
        Verification = new VerificationPolicyDto(),
        Escalation = new EscalationPolicyDto(),
        FeatureFlags = new[]
        {
            new FeatureFlagDto { Key = "citizen.community", Description = "Citizen public issue and following features.", Enabled = true, Audience = "Citizen" },
            new FeatureFlagDto { Key = "ai.triage", Description = "AI-assisted complaint triage.", Enabled = true, Audience = "Staff" },
            new FeatureFlagDto { Key = "contractor.management", Description = "Contractor workflow pending a database relationship.", Enabled = false, Audience = "Admin" }
        },
        Authorities = Array.Empty<AuthorityDto>(),
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static void ValidateMasterData(AdminMasterDataConfigurationDto request)
    {
        if (request.Priorities.Count == 0) throw new BusinessRuleViolationException("At least one priority rule is required.");
        var expectedPriorities = Enum.GetNames<ComplaintPriority>();
        if (request.Priorities.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Priorities.Count)
            throw new BusinessRuleViolationException("Priority names must be unique.");
        if (request.Priorities.Any(x => !expectedPriorities.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException("Priority rules can configure only existing Low, Medium, High and Critical values.");
        if (request.Priorities.Any(x => string.IsNullOrWhiteSpace(x.DisplayName) || x.AssignmentHours is < 1 or > 8760 || x.ResolutionHours is < 1 or > 17520 || x.AssignmentHours > x.ResolutionHours))
            throw new BusinessRuleViolationException("Priority SLA hours are invalid.");

        var expectedStatuses = Enum.GetNames<ComplaintStatus>();
        if (request.Statuses.Count != expectedStatuses.Length ||
            request.Statuses.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Statuses.Count ||
            request.Statuses.Any(x => !expectedStatuses.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException("Status configuration must contain each existing complaint status exactly once.");
        if (request.Statuses.Any(x => string.IsNullOrWhiteSpace(x.DisplayName)))
            throw new BusinessRuleViolationException("Every status requires a display name.");

        var verification = request.Verification;
        if (verification.MediumWindowHours is < 1 or > 720 || verification.HighWindowHours is < 1 or > 720 ||
            verification.CriticalWindowHours is < 1 or > 720 || verification.AppealGraceDays is < 1 or > 30 ||
            verification.GeoFenceMeters is < 50 or > 5000)
            throw new BusinessRuleViolationException("Verification policy values are outside their allowed ranges.");

        var escalation = request.Escalation;
        if (escalation.AtRiskPercent is < 50 or > 99 || escalation.EscalationGraceHours is < 1 or > 168 ||
            escalation.MaximumAutomaticEscalations is < 1 or > 20)
            throw new BusinessRuleViolationException("Escalation policy values are outside their allowed ranges.");

        if (request.FeatureFlags.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.FeatureFlags.Count ||
            request.FeatureFlags.Any(x => !FeatureKeyPattern.IsMatch(x.Key) || x.Description.Trim().Length is < 3 or > 300))
            throw new BusinessRuleViolationException("Feature flags require unique valid keys and descriptions.");
        if (request.Authorities.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Authorities.Count ||
            request.Authorities.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name) || x.Code.Length > 40 || x.Name.Length > 150))
            throw new BusinessRuleViolationException("Authorities require unique codes and valid names.");
    }

    private static AdminManagedUserDto Map(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.Role.ToString(),
        user.DepartmentId, user.Department?.Name, user.WardId, user.Ward?.Name,
        user.IsActive, user.IsEmailVerified, user.IsPhoneVerified, user.IsDeleted,
        user.DeletedAt, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);

    private void AddAudit(string action, long entityId, object? oldValues, object? newValues, string entityName = "User") =>
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = action,
            EntityName = entityName,
            EntityId = entityId <= 0 ? null : entityId.ToString(),
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            Severity = "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow
        });

    private UserRole RequireActorRole() => Enum.TryParse<UserRole>(_currentUser.Role, true, out var role)
        ? role : throw new UnauthorizedAccessException("Authenticated role is invalid.");
    private long RequireActorId() => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
    private void EnsureSuperAdmin()
    {
        if (RequireActorRole() != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can update global master-data policy.");
    }
    private static void EnsureNotDeleted(User user)
    {
        if (user.IsDeleted) throw new BusinessRuleViolationException("Restore the account before modifying it.");
    }
    private static void InvalidateSessions(User user)
    {
        user.AuthorizationVersion++;
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
    }
    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 10 or > 1000)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["An audited reason containing 10–1000 characters is required."]);
    }
    private static void ValidatePassword(string password)
    {
        var errors = new List<string>();
        if (password.Length is < 8 or > 128) errors.Add("Password must contain 8–128 characters.");
        if (!password.Any(char.IsUpper)) errors.Add("Password must contain an uppercase letter.");
        if (!password.Any(char.IsLower)) errors.Add("Password must contain a lowercase letter.");
        if (!password.Any(char.IsDigit)) errors.Add("Password must contain a number.");
        if (!password.Any(ch => !char.IsLetterOrDigit(ch))) errors.Add("Password must contain a special character.");
        if (errors.Count > 0) throw new CivicHero.Backend.Core.Exceptions.ValidationException(errors);
    }
    private static string GenerateTemporaryPassword() => $"CvH@{RandomNumberGenerator.GetInt32(10, 99)}{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}a";
    private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static bool IsValidEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }
    private static string Category(string action) => action.Contains("ROLE", StringComparison.OrdinalIgnoreCase) || action.Contains("ChangeRole", StringComparison.OrdinalIgnoreCase)
        ? "Role" : action.Contains("EMAIL", StringComparison.OrdinalIgnoreCase) ? "Email"
        : action.Contains("DELETE", StringComparison.OrdinalIgnoreCase) || action.Contains("RESTORE", StringComparison.OrdinalIgnoreCase) ? "Lifecycle"
        : action.Contains("LOGIN", StringComparison.OrdinalIgnoreCase) || action.Contains("SESSION", StringComparison.OrdinalIgnoreCase) ? "Security"
        : "Account";
    private static string SplitWords(string value) => Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");

    private sealed class DepartmentHeadAssignmentState
    {
        public long DepartmentId { get; set; }
        public long UserId { get; set; }
        public DateTimeOffset AssignedAt { get; set; }
        public long AssignedByUserId { get; set; }
    }
}
