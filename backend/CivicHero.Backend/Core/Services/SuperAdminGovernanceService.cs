using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Validation;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CivicHero.Backend.Core.Services;

public sealed class SuperAdminGovernanceService : ISuperAdminGovernanceService
{
    public const string RolePolicyKey = "Governance.RolePolicy";
    public const string AuthenticationPolicyKey = "Governance.AuthenticationPolicy";
    public const string TwoFactorExemptionsKey = "Governance.TwoFactorEnrollmentExemptions";
    public const string RolePolicyCacheKey = "Governance.RolePolicy.Runtime";
    private const string ReleaseDecisionPrefix = "Governance.ReleaseDecision.";
    private const string ReleaseEntityName = "ReleaseGovernance";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _memoryCache;

    public SuperAdminGovernanceService(
        CivicDbContext db,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        IWebHostEnvironment environment,
        IMemoryCache memoryCache)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _environment = environment;
        _memoryCache = memoryCache;
    }

    public async Task<IReadOnlyList<SuperAdminAdminAccountDto>> GetAdminAccountsAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        var query = _db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Role == UserRole.Admin && !x.IsDeleted);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.FullName.Contains(term) || x.Email.Contains(term) || (x.Phone != null && x.Phone.Contains(term)));
        }
        var now = DateTimeOffset.UtcNow;
        return await query.OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .Select(x => new SuperAdminAdminAccountDto(
                x.Id, x.FullName, x.Email, x.Phone, x.IsActive, x.IsEmailVerified,
                x.TwoFactorEnabled, x.TwoFactorEnabledAt, x.LastLoginAt,
                _db.UserSessions.Any(session => session.UserId == x.Id && !session.RevokedAt.HasValue &&
                    session.ExpiresAt > now && session.AuthorizationVersion == x.AuthorizationVersion),
                x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SuperAdminCreateAdminResponse> CreateAdminAsync(
        SuperAdminCreateAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        var fullName = PersonNameRules.Normalize(request.FullName);
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["A valid Admin email address is required."]);
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
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            NormalizedPhone = phone,
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true,
            IsEmailVerified = request.MarkEmailVerified,
            IsPhoneVerified = false,
            AuthorizationVersion = 1,
            EmailVerificationTokenHash = verificationToken is null ? null : HashToken(verificationToken),
            EmailVerificationTokenExpiresAt = verificationToken is null ? null : now.AddHours(24)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var authPolicy = await ReadAuthenticationPolicyAsync(cancellationToken);
        DateTimeOffset? enrollmentDeadline = null;
        if (authPolicy.TwoFactorRequiredRoles.Contains(UserRole.Admin.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            enrollmentDeadline = now.AddHours(authPolicy.TwoFactorEnrollmentGraceHours);
            await UpsertTwoFactorExemptionAsync(user.Id, enrollmentDeadline.Value, cancellationToken);
        }

        AddAudit("SUPERADMIN_ADMIN_CREATED", "User", user.Id.ToString(), null, new
        {
            user.Id, user.FullName, user.Email, role = user.Role.ToString(), user.IsActive,
            user.IsEmailVerified, twoFactorEnrollmentDeadlineUtc = enrollmentDeadline
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new SuperAdminCreateAdminResponse(
            MapAdmin(user),
            password,
            !user.IsEmailVerified,
            user.EmailVerificationTokenExpiresAt,
            _environment.IsDevelopment() ? verificationToken : null,
            enrollmentDeadline);
    }

    public async Task<SuperAdminAdminAccountDto> SetAdminActiveAsync(
        long userId,
        SuperAdminAdminAccountLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        ValidateReason(request.Reason);
        var user = await LoadAdminAsync(userId, cancellationToken);
        if (user.IsActive == request.IsActive)
            throw new BusinessRuleViolationException($"This Admin account is already {(request.IsActive ? "active" : "inactive")}.");
        var old = new { user.IsActive };
        user.IsActive = request.IsActive;
        InvalidateSessions(user);
        AddAudit(request.IsActive ? "SUPERADMIN_ADMIN_ACTIVATED" : "SUPERADMIN_ADMIN_DEACTIVATED", "User", user.Id.ToString(), old,
            new { user.IsActive, request.Reason });
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(user);
    }

    public async Task<SuperAdminResetAdminPasswordResponse> ResetAdminPasswordAsync(
        long userId,
        SuperAdminResetAdminPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        ValidateReason(request.Reason);
        var user = await LoadAdminAsync(userId, cancellationToken);
        var password = string.IsNullOrWhiteSpace(request.TemporaryPassword)
            ? GenerateTemporaryPassword()
            : request.TemporaryPassword;
        ValidatePassword(password);
        user.PasswordHash = _passwordHasher.Hash(password);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        InvalidateSessions(user);
        AddAudit("SUPERADMIN_ADMIN_PASSWORD_RESET", "User", user.Id.ToString(), null,
            new { request.Reason, sessionsRevoked = true });
        await _db.SaveChangesAsync(cancellationToken);
        return new SuperAdminResetAdminPasswordResponse(user.Id, user.Email, password, DateTimeOffset.UtcNow);
    }

    public async Task<SuperAdminGovernancePoliciesDto> GetPoliciesAsync(CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        return new SuperAdminGovernancePoliciesDto(
            await ReadRolePolicyAsync(cancellationToken),
            await ReadAuthenticationPolicyAsync(cancellationToken));
    }

    public async Task<RolePolicyConfigurationDto> UpdateRolePolicyAsync(
        RolePolicyConfigurationDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        ValidateRolePolicy(request);
        var normalized = new RolePolicyConfigurationDto
        {
            Roles = request.Roles.Select(x => new RolePolicyEntryDto
            {
                Role = Enum.Parse<UserRole>(x.Role, true).ToString(),
                Description = x.Description.Trim(),
                Enabled = x.Enabled,
                DeniedApiPrefixes = x.DeniedApiPrefixes
                    .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
                    .Select(NormalizeApiPrefix)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(prefix => prefix)
                    .ToArray()
            }).OrderBy(x => Enum.Parse<UserRole>(x.Role)).ToArray(),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedByUserId = RequireActorId()
        };
        var old = await ReadRolePolicyAsync(cancellationToken);
        await WriteSettingAsync(RolePolicyKey, normalized, "Governance", "Deny-only runtime role policy layered on top of controller authorization.", cancellationToken);
        await RevokeAllSessionsAsync(cancellationToken);
        AddAudit("SUPERADMIN_ROLE_POLICY_UPDATED", "SystemSetting", RolePolicyKey, old, normalized);
        await _db.SaveChangesAsync(cancellationToken);
        _memoryCache.Remove(RolePolicyCacheKey);
        return normalized;
    }

    public async Task<AuthenticationPolicyDto> UpdateAuthenticationPolicyAsync(
        AuthenticationPolicyDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        ValidateAuthenticationPolicy(request);
        var normalizedRoles = request.TwoFactorRequiredRoles
            .Select(role => Enum.Parse<UserRole>(role, true).ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => Enum.Parse<UserRole>(role))
            .ToArray();
        var normalized = new AuthenticationPolicyDto
        {
            MaximumFailedLoginAttempts = request.MaximumFailedLoginAttempts,
            LockoutMinutes = request.LockoutMinutes,
            RequireVerifiedEmail = request.RequireVerifiedEmail,
            TwoFactorRequiredRoles = normalizedRoles,
            TwoFactorEnrollmentGraceHours = request.TwoFactorEnrollmentGraceHours,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedByUserId = RequireActorId()
        };
        var old = await ReadAuthenticationPolicyAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var requiredRoleValues = normalizedRoles.Select(role => Enum.Parse<UserRole>(role)).ToArray();
        var nonCompliant = await _db.Users
            .Where(x => x.IsActive && !x.IsDeleted && requiredRoleValues.Contains(x.Role) && !x.TwoFactorEnabled)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        foreach (var userId in nonCompliant)
            await UpsertTwoFactorExemptionAsync(userId, now.AddHours(normalized.TwoFactorEnrollmentGraceHours), cancellationToken);

        await WriteSettingAsync(AuthenticationPolicyKey, normalized, "Security", "Global login lockout, verified-email and role-based two-factor policy.", cancellationToken);
        await RevokeAllSessionsAsync(cancellationToken);
        AddAudit("SUPERADMIN_AUTHENTICATION_POLICY_UPDATED", "SystemSetting", AuthenticationPolicyKey, old, new
        {
            policy = normalized,
            enrollmentGraceAppliedToUsers = nonCompliant.Count
        });
        await _db.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<GlobalSessionListDto> GetGlobalSessionsAsync(
        string? search,
        string? role,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        var now = DateTimeOffset.UtcNow;
        var query = _db.UserSessions.AsNoTracking()
            .Where(x => !x.RevokedAt.HasValue && x.ExpiresAt > now &&
                x.AuthorizationVersion == x.User.AuthorizationVersion && x.User.IsActive && !x.User.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.User.FullName.Contains(term) || x.User.Email.Contains(term) ||
                x.DeviceLabel.Contains(term) || (x.IpAddress != null && x.IpAddress.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
                throw new CivicHero.Backend.Core.Exceptions.ValidationException(["The session role filter is invalid."]);
            query = query.Where(x => x.User.Role == parsedRole);
        }

        var sessions = await query.OrderByDescending(x => x.LastSeenAt).ThenByDescending(x => x.CreatedAt)
            .Take(1000)
            .Select(x => new GlobalSessionDto(
                x.SessionId,
                x.UserId,
                x.User.FullName,
                x.User.Email,
                x.User.Role.ToString(),
                x.DeviceLabel,
                x.IpAddress,
                x.UserAgent,
                x.CreatedAt,
                x.ExpiresAt,
                x.LastSeenAt,
                x.User.LastLoginAt,
                x.User.TwoFactorEnabled))
            .ToListAsync(cancellationToken);

        return new GlobalSessionListDto(true,
            "Each active browser or device has an independently revocable refresh session.",
            sessions, now);
    }

    public async Task<GlobalSessionRevocationDto> RevokeGlobalSessionAsync(
        string sessionId,
        RevokeGlobalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        ValidateReason(request.Reason);
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Session identifier is required."]);

        var now = DateTimeOffset.UtcNow;
        var session = await _db.UserSessions.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.SessionId == sessionId.Trim() && !x.RevokedAt.HasValue &&
                x.ExpiresAt > now && x.AuthorizationVersion == x.User.AuthorizationVersion, cancellationToken)
            ?? throw new NotFoundException("The selected global session was not found or has expired.");

        session.RevokedAt = now;
        session.RevokedReason = request.Reason.Trim();
        AddAudit("SUPERADMIN_GLOBAL_SESSION_REVOKED", "SecuritySession", session.SessionId, null,
            new { userId = session.UserId, session.User.Email, role = session.User.Role.ToString(), request.Reason });
        await _db.SaveChangesAsync(cancellationToken);
        return new GlobalSessionRevocationDto(session.SessionId, session.UserId, session.User.Email,
            "Selected global session revoked", now);
    }

    public async Task<ReleaseGovernanceOverviewDto> GetReleaseDecisionsAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        take = Math.Clamp(take, 1, 500);
        var current = new List<ReleaseGovernanceDecisionDto>();
        foreach (var target in new[] { "Launch", "FinalRelease" })
        {
            var setting = await _db.SystemSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Key == ReleaseDecisionPrefix + target, cancellationToken);
            var value = setting is null ? null : DeserializeOrDefault<ReleaseGovernanceDecisionDto?>(setting.Value, null);
            if (value is not null) current.Add(value);
        }
        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.EntityName == ReleaseEntityName)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        var history = logs.Select(MapReleaseHistory).Where(x => x is not null).Cast<ReleaseGovernanceHistoryItemDto>().ToArray();
        return new ReleaseGovernanceOverviewDto(current, history, DateTimeOffset.UtcNow);
    }

    public async Task<ReleaseGovernanceDecisionDto> DecideReleaseAsync(
        string target,
        ReleaseGovernanceDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();
        var normalizedTarget = NormalizeReleaseTarget(target);
        var decision = NormalizeDecision(request.Decision);
        ValidateReason(request.Reason);
        var evidenceValidated = decision == "Reject" || ValidateReleaseEvidence(normalizedTarget);
        var result = new ReleaseGovernanceDecisionDto(
            normalizedTarget,
            decision,
            request.Reason.Trim(),
            string.IsNullOrWhiteSpace(request.ReleaseVersion) ? null : request.ReleaseVersion.Trim(),
            string.IsNullOrWhiteSpace(request.EvidenceReference) ? DefaultEvidenceReference(normalizedTarget) : request.EvidenceReference.Trim(),
            RequireActorId(),
            _currentUser.Email,
            DateTimeOffset.UtcNow,
            evidenceValidated);
        await WriteSettingAsync(ReleaseDecisionPrefix + normalizedTarget, result, "Release",
            $"Current SuperAdmin governance decision for {normalizedTarget}.", cancellationToken);
        AddAudit($"SUPERADMIN_{normalizedTarget.ToUpperInvariant()}_{decision.ToUpperInvariant()}", ReleaseEntityName, normalizedTarget, null, result);
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<User> LoadAdminAsync(long userId, CancellationToken cancellationToken) =>
        await _db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == userId && x.Role == UserRole.Admin && !x.IsDeleted, cancellationToken)
        ?? throw new NotFoundException("Admin account was not found.");

    private async Task<RolePolicyConfigurationDto> ReadRolePolicyAsync(CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == RolePolicyKey, cancellationToken);
        return setting is null ? BuildDefaultRolePolicy() : DeserializeOrDefault(setting.Value, BuildDefaultRolePolicy());
    }

    private async Task<AuthenticationPolicyDto> ReadAuthenticationPolicyAsync(CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == AuthenticationPolicyKey, cancellationToken);
        return setting is null ? BuildDefaultAuthenticationPolicy() : DeserializeOrDefault(setting.Value, BuildDefaultAuthenticationPolicy());
    }

    private async Task UpsertTwoFactorExemptionAsync(long userId, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var setting = await GetOrCreateSettingAsync(TwoFactorExemptionsKey, Array.Empty<TwoFactorEnrollmentExemptionDto>(), "Security",
            "Temporary role-based two-factor enrollment grace periods.", cancellationToken);
        var exemptions = DeserializeOrDefault<IReadOnlyList<TwoFactorEnrollmentExemptionDto>>(setting.Value, Array.Empty<TwoFactorEnrollmentExemptionDto>())
            .Where(x => x.UserId != userId && x.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .Append(new TwoFactorEnrollmentExemptionDto { UserId = userId, ExpiresAtUtc = expiresAtUtc })
            .OrderBy(x => x.UserId)
            .ToArray();
        setting.Value = JsonSerializer.Serialize(exemptions, JsonOptions);
        setting.UpdatedByUserId = RequireActorId();
    }

    private async Task WriteSettingAsync<T>(string key, T value, string group, string description, CancellationToken cancellationToken)
    {
        var setting = await GetOrCreateSettingAsync(key, value, group, description, cancellationToken);
        setting.Value = JsonSerializer.Serialize(value, JsonOptions);
        setting.ValueType = "Json";
        setting.Group = group;
        setting.Description = description;
        setting.IsPublic = false;
        setting.IsSensitive = false;
        setting.UpdatedByUserId = RequireActorId();
    }

    private async Task<SystemSetting> GetOrCreateSettingAsync<T>(
        string key,
        T defaultValue,
        string group,
        string description,
        CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting is not null) return setting;
        setting = new SystemSetting
        {
            Key = key,
            Value = JsonSerializer.Serialize(defaultValue, JsonOptions),
            ValueType = "Json",
            Description = description,
            Group = group,
            IsPublic = false,
            IsSensitive = false,
            UpdatedByUserId = _currentUser.UserId
        };
        _db.SystemSettings.Add(setting);
        return setting;
    }

    private async Task RevokeAllSessionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = await _db.UserSessions
            .Where(x => !x.RevokedAt.HasValue && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.RevokedReason = "Revoked by global governance policy change";
        }

        var userIds = sessions.Select(x => x.UserId).Distinct().ToArray();
        var users = await _db.Users
            .Where(x => userIds.Contains(x.Id) || x.RefreshTokenHash != null)
            .ToListAsync(cancellationToken);
        foreach (var user in users) InvalidateSessions(user);
    }

    private void AddAudit(string action, string entityName, string? entityId, object? oldValues, object? newValues) =>
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            Severity = "Warning",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow
        });

    private bool ValidateReleaseEvidence(string target)
    {
        var root = ResolveProjectRoot();
        if (target == "FinalRelease")
        {
            var path = Path.Combine(root, "artifacts", "phase18", "release-evidence.json");
            if (!File.Exists(path)) throw new BusinessRuleViolationException("Final release approval requires artifacts/phase18/release-evidence.json.");
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var rootElement = document.RootElement;
                var approved = TryGetPropertyIgnoreCase(rootElement, "approved", out var approvedNode) && approvedNode.ValueKind == JsonValueKind.True;
                if (!approved) throw new BusinessRuleViolationException("Phase 18 release evidence has not approved the candidate.");
                if (!TryGetPropertyIgnoreCase(rootElement, "gates", out var gates) || gates.ValueKind != JsonValueKind.Array)
                    throw new BusinessRuleViolationException("Phase 18 release evidence does not contain gate results.");
                var required = gates.EnumerateArray().Where(gate => !TryGetPropertyIgnoreCase(gate, "required", out var node) || node.ValueKind != JsonValueKind.False).ToArray();
                if (required.Length == 0 || required.Any(gate => !TryGetPropertyIgnoreCase(gate, "status", out var status) || !string.Equals(status.GetString(), "Passed", StringComparison.OrdinalIgnoreCase)))
                    throw new BusinessRuleViolationException("Every required Phase 18 release gate must have Passed evidence before approval.");
                return true;
            }
            catch (JsonException)
            {
                throw new BusinessRuleViolationException("Phase 18 release evidence is not valid JSON.");
            }
        }

        var requiredDocs = new[]
        {
            "docs/requirements/SRS.md", "docs/architecture/HLD.md", "docs/architecture/LLD.md",
            "docs/database/DATABASE-DESIGN.md", "docs/api/API-REFERENCE.md",
            "docs/training/CITIZEN-TRAINING.md", "docs/training/OFFICER-TRAINING.md",
            "docs/training/SUPERVISOR-TRAINING.md", "docs/training/ADMIN-TRAINING.md",
            "docs/operations/MAINTENANCE-PLAN.md", "docs/operations/INCIDENT-RESPONSE.md",
            "docs/handover/PROJECT-HANDOVER.md", "docs/FINAL-PROJECT-REPORT.md", "docs/FUTURE-ROADMAP.md"
        };
        var missing = requiredDocs.Where(path => !File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))).ToArray();
        if (missing.Length > 0)
            throw new BusinessRuleViolationException($"Launch approval is blocked because {missing.Length} required handover document(s) are missing.");
        var launchEvidencePath = Path.Combine(root, "artifacts", "phase16", "launch-approval-evidence.json");
        if (!File.Exists(launchEvidencePath))
            throw new BusinessRuleViolationException("Launch approval requires artifacts/phase16/launch-approval-evidence.json with real stakeholder sign-off.");
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(launchEvidencePath));
            if (!TryGetPropertyIgnoreCase(document.RootElement, "approved", out var approved) || approved.ValueKind != JsonValueKind.True)
                throw new BusinessRuleViolationException("Launch evidence does not contain approved=true stakeholder sign-off.");
            return true;
        }
        catch (JsonException)
        {
            throw new BusinessRuleViolationException("Launch approval evidence is not valid JSON.");
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    private string ResolveProjectRoot()
    {
        var directory = new DirectoryInfo(_environment.ContentRootPath);
        return directory.Parent?.Parent?.FullName ?? _environment.ContentRootPath;
    }

    private static string DefaultEvidenceReference(string target) => target == "FinalRelease"
        ? "artifacts/phase18/release-evidence.json"
        : "artifacts/phase16/launch-approval-evidence.json";

    private static string NormalizeReleaseTarget(string target) => target.Trim().ToLowerInvariant() switch
    {
        "launch" => "Launch",
        "finalrelease" or "final-release" or "final_release" => "FinalRelease",
        _ => throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Release decision target must be Launch or FinalRelease."])
    };

    private static string NormalizeDecision(string decision) => decision.Trim().ToLowerInvariant() switch
    {
        "approve" or "approved" => "Approve",
        "reject" or "rejected" => "Reject",
        _ => throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Decision must be Approve or Reject."])
    };

    private static ReleaseGovernanceHistoryItemDto? MapReleaseHistory(AuditLog log)
    {
        if (string.IsNullOrWhiteSpace(log.NewValuesJson)) return null;
        try
        {
            var value = JsonSerializer.Deserialize<ReleaseGovernanceDecisionDto>(log.NewValuesJson, JsonOptions);
            return value is null ? null : new ReleaseGovernanceHistoryItemDto(
                log.Id, value.Target, value.Decision, value.Reason, value.ReleaseVersion,
                value.EvidenceReference, log.UserId, log.UserEmail, log.CreatedAt, value.EvidenceValidated);
        }
        catch (JsonException) { return null; }
    }

    private static SuperAdminAdminAccountDto MapAdmin(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone, user.IsActive, user.IsEmailVerified,
        user.TwoFactorEnabled, user.TwoFactorEnabledAt, user.LastLoginAt,
        false,
        user.CreatedAt, user.UpdatedAt);

    private static RolePolicyConfigurationDto BuildDefaultRolePolicy() => new()
    {
        Roles = Enum.GetValues<UserRole>().Select(role => new RolePolicyEntryDto
        {
            Role = role.ToString(),
            Description = role switch
            {
                UserRole.Citizen => "Citizen self-service and public community operations.",
                UserRole.Officer => "Assigned field-work and evidence operations.",
                UserRole.Supervisor => "Department and ward operational oversight.",
                UserRole.Admin => "Administration and governance operations.",
                UserRole.SuperAdmin => "Global governance and final approval operations.",
                _ => role.ToString()
            },
            Enabled = true,
            DeniedApiPrefixes = Array.Empty<string>()
        }).ToArray(),
        UpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private static AuthenticationPolicyDto BuildDefaultAuthenticationPolicy() => new()
    {
        MaximumFailedLoginAttempts = 5,
        LockoutMinutes = 30,
        RequireVerifiedEmail = true,
        TwoFactorRequiredRoles = Array.Empty<string>(),
        TwoFactorEnrollmentGraceHours = 24,
        UpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private static void ValidateRolePolicy(RolePolicyConfigurationDto request)
    {
        var expected = Enum.GetNames<UserRole>();
        request.Roles ??= Array.Empty<RolePolicyEntryDto>();
        if (request.Roles.Count != expected.Length ||
            request.Roles.Select(x => x.Role).Distinct(StringComparer.OrdinalIgnoreCase).Count() != expected.Length ||
            request.Roles.Any(x => !expected.Contains(x.Role, StringComparer.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException("Role policy must contain every existing role exactly once.");
        foreach (var entry in request.Roles)
        {
            entry.DeniedApiPrefixes ??= Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(entry.Description) || entry.Description.Trim().Length > 300)
                throw new BusinessRuleViolationException("Every role policy requires a description of at most 300 characters.");
            if (entry.DeniedApiPrefixes.Count > 50)
                throw new BusinessRuleViolationException("A role cannot contain more than 50 denied API prefixes.");
            foreach (var prefix in entry.DeniedApiPrefixes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var normalized = NormalizeApiPrefix(prefix);
                if (normalized.Length > 150) throw new BusinessRuleViolationException("Denied API prefixes cannot exceed 150 characters.");
            }
        }
        var superAdmin = request.Roles.Single(x => x.Role.Equals(UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase));
        if (!superAdmin.Enabled) throw new BusinessRuleViolationException("The SuperAdmin role cannot be globally disabled.");
        if (superAdmin.DeniedApiPrefixes.Select(NormalizeApiPrefix).Any(prefix =>
            "/api/v1/superadmin".StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            "/api/v1/security".StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException("SuperAdmin governance and security routes cannot be denied to SuperAdmin.");
    }

    private static void ValidateAuthenticationPolicy(AuthenticationPolicyDto request)
    {
        if (request.MaximumFailedLoginAttempts is < 3 or > 20)
            throw new BusinessRuleViolationException("Maximum failed login attempts must be between 3 and 20.");
        if (request.LockoutMinutes is < 5 or > 1440)
            throw new BusinessRuleViolationException("Lockout duration must be between 5 and 1440 minutes.");
        if (request.TwoFactorEnrollmentGraceHours is < 1 or > 168)
            throw new BusinessRuleViolationException("Two-factor enrollment grace must be between 1 and 168 hours.");
        request.TwoFactorRequiredRoles ??= Array.Empty<string>();
        var expected = Enum.GetNames<UserRole>();
        if (request.TwoFactorRequiredRoles.Any(role => string.IsNullOrWhiteSpace(role) || !expected.Contains(role, StringComparer.OrdinalIgnoreCase)))
            throw new BusinessRuleViolationException("Two-factor policy contains an unknown role.");
    }

    private static string NormalizeApiPrefix(string prefix)
    {
        var value = prefix.Trim();
        if (!value.StartsWith('/')) value = "/" + value;
        if (!value.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase) && !value.Equals("/api/v1", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("Denied route prefixes must begin with /api/v1/.");
        return value.TrimEnd('/').ToLowerInvariant();
    }

    private static void InvalidateSessions(User user)
    {
        user.AuthorizationVersion = checked(user.AuthorizationVersion + 1);
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
    }

    private void EnsureSuperAdmin()
    {
        if (!string.Equals(_currentUser.Role, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Only a SuperAdmin can perform this governance operation.");
    }

    private long RequireActorId() => _currentUser.UserId
        ?? throw new UnauthorizedAccessException("Authenticated SuperAdmin identifier is missing.");

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
        try { return new MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }

    private static T DeserializeOrDefault<T>(string json, T fallback)
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback; }
        catch (JsonException) { return fallback; }
    }
}
