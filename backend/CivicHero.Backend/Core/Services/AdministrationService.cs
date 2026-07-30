using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Entities;
using ComplaintCategoryEntity = CivicHero.Backend.Core.Entities.ComplaintCategory;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CivicHero.Backend.Core.Services;

public sealed class AdministrationService : IAdministrationService
{
    private static readonly string[] PriorityNames = ["Low", "Medium", "High", "Critical"];
    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly HealthCheckService _healthChecks;
    private readonly IHostEnvironment _environment;

    public AdministrationService(CivicDbContext db, ICurrentUserService currentUser,
        HealthCheckService healthChecks, IHostEnvironment environment)
    {
        _db = db;
        _currentUser = currentUser;
        _healthChecks = healthChecks;
        _environment = environment;
    }

    public async Task<AdministrationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        return new AdministrationOverviewDto(
            await _db.Departments.CountAsync(x => x.IsActive, cancellationToken),
            await _db.Wards.CountAsync(x => x.IsActive, cancellationToken),
            await _db.ComplaintCategories.CountAsync(x => x.IsActive, cancellationToken),
            await _db.Users.CountAsync(x => x.IsActive, cancellationToken),
            await _db.Complaints.CountAsync(x => x.Status != ComplaintStatus.Closed &&
                x.Status != ComplaintStatus.ClosedAuto && x.Status != ComplaintStatus.ClosedFraud &&
                x.Status != ComplaintStatus.Withdrawn && x.Status != ComplaintStatus.Merged, cancellationToken),
            await _db.AuditLogs.CountAsync(x => x.CreatedAt >= today, cancellationToken),
            DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultCategoriesAsync(cancellationToken);
        return await _db.ComplaintCategories.IgnoreQueryFilters().AsNoTracking()
            .Include(x => x.Department).Where(x => !x.IsDeleted)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Code, x.Description, x.DefaultPriority,
                x.Icon, x.DepartmentId, x.Department == null ? null : x.Department.Name,
                x.IsActive, x.SortOrder, x.UpdatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> CreateCategoryAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCategory(request);
        var code = NormalizeCode(request.Code);
        if (await _db.ComplaintCategories.IgnoreQueryFilters().AnyAsync(x => x.Code == code, cancellationToken))
            throw new BusinessRuleViolationException("A complaint category with this code already exists.");
        await EnsureDepartmentAsync(request.DepartmentId, cancellationToken);
        var entity = new ComplaintCategoryEntity
        {
            Name = request.Name.Trim(), Code = code, Description = Clean(request.Description),
            DefaultPriority = request.DefaultPriority.Trim(), Icon = Clean(request.Icon),
            DepartmentId = request.DepartmentId, IsActive = request.IsActive, SortOrder = request.SortOrder
        };
        _db.ComplaintCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetCategoriesAsync(cancellationToken)).Single(x => x.Id == entity.Id);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(long id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCategory(request);
        var entity = await _db.ComplaintCategories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Complaint category was not found.");
        var code = NormalizeCode(request.Code);
        if (await _db.ComplaintCategories.IgnoreQueryFilters().AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new BusinessRuleViolationException("A complaint category with this code already exists.");
        await EnsureDepartmentAsync(request.DepartmentId, cancellationToken);
        entity.Name = request.Name.Trim(); entity.Code = code; entity.Description = Clean(request.Description);
        entity.DefaultPriority = request.DefaultPriority.Trim(); entity.Icon = Clean(request.Icon);
        entity.DepartmentId = request.DepartmentId; entity.IsActive = request.IsActive; entity.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetCategoriesAsync(cancellationToken)).Single(x => x.Id == id);
    }

    public async Task SetCategoryActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ComplaintCategories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Complaint category was not found.");
        entity.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DepartmentAdminDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var open = OpenComplaintStatuses();
        return await _db.Departments.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new DepartmentAdminDto(x.Id, x.Name, x.Code, x.Description, x.IsActive,
                _db.Wards.Count(w => w.DepartmentId == x.Id && w.IsActive),
                _db.Users.Count(u => u.DepartmentId == x.Id && u.IsActive),
                _db.Complaints.Count(c => c.DepartmentId == x.Id && open.Contains(c.Status)), x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentAdminDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateNameCode(request.Name, request.Code);
        var code = NormalizeCode(request.Code);
        if (await _db.Departments.IgnoreQueryFilters().AnyAsync(x => x.Code == code, cancellationToken))
            throw new BusinessRuleViolationException("A department with this code already exists.");
        var entity = new Department { Name = request.Name.Trim(), Code = code, Description = Clean(request.Description), IsActive = request.IsActive };
        _db.Departments.Add(entity); await _db.SaveChangesAsync(cancellationToken);
        return (await GetDepartmentsAsync(cancellationToken)).Single(x => x.Id == entity.Id);
    }

    public async Task<DepartmentAdminDto> UpdateDepartmentAsync(long id, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateNameCode(request.Name, request.Code);
        var entity = await _db.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Department was not found.");
        var code = NormalizeCode(request.Code);
        if (await _db.Departments.IgnoreQueryFilters().AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new BusinessRuleViolationException("A department with this code already exists.");
        entity.Name = request.Name.Trim(); entity.Code = code; entity.Description = Clean(request.Description); entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetDepartmentsAsync(cancellationToken)).Single(x => x.Id == id);
    }

    public async Task SetDepartmentActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Department was not found.");
        entity.IsActive = isActive;
        if (!isActive)
        {
            var wards = await _db.Wards.Where(x => x.DepartmentId == id).ToListAsync(cancellationToken);
            wards.ForEach(x => x.IsActive = false);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WardAdminDto>> GetWardsAsync(long? departmentId, CancellationToken cancellationToken = default)
    {
        var open = OpenComplaintStatuses();
        var query = _db.Wards.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsDeleted);
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId.Value);
        return await query.OrderBy(x => x.Department.Name).ThenBy(x => x.Name)
            .Select(x => new WardAdminDto(x.Id, x.DepartmentId, x.Department.Name, x.Name, x.Code,
                x.BoundaryNorth, x.BoundarySouth, x.BoundaryEast, x.BoundaryWest, x.IsActive,
                _db.Users.Count(u => u.WardId == x.Id && u.IsActive),
                _db.Complaints.Count(c => c.WardId == x.Id && open.Contains(c.Status)), x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<WardAdminDto> CreateWardAsync(SaveWardRequest request, CancellationToken cancellationToken = default)
    {
        ValidateWard(request); await EnsureDepartmentAsync(request.DepartmentId, cancellationToken);
        var code = NormalizeCode(request.Code);
        if (await _db.Wards.IgnoreQueryFilters().AnyAsync(x => x.Code == code, cancellationToken))
            throw new BusinessRuleViolationException("A ward with this code already exists.");
        var entity = new Ward { DepartmentId = request.DepartmentId, Name = request.Name.Trim(), Code = code,
            BoundaryNorth = request.BoundaryNorth, BoundarySouth = request.BoundarySouth,
            BoundaryEast = request.BoundaryEast, BoundaryWest = request.BoundaryWest, IsActive = request.IsActive };
        _db.Wards.Add(entity); await _db.SaveChangesAsync(cancellationToken);
        return (await GetWardsAsync(null, cancellationToken)).Single(x => x.Id == entity.Id);
    }

    public async Task<WardAdminDto> UpdateWardAsync(long id, SaveWardRequest request, CancellationToken cancellationToken = default)
    {
        ValidateWard(request); await EnsureDepartmentAsync(request.DepartmentId, cancellationToken);
        var entity = await _db.Wards.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ward was not found.");
        var code = NormalizeCode(request.Code);
        if (await _db.Wards.IgnoreQueryFilters().AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            throw new BusinessRuleViolationException("A ward with this code already exists.");
        entity.DepartmentId = request.DepartmentId; entity.Name = request.Name.Trim(); entity.Code = code;
        entity.BoundaryNorth = request.BoundaryNorth; entity.BoundarySouth = request.BoundarySouth;
        entity.BoundaryEast = request.BoundaryEast; entity.BoundaryWest = request.BoundaryWest; entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetWardsAsync(null, cancellationToken)).Single(x => x.Id == id);
    }

    public async Task SetWardActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Wards.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ward was not found.");
        entity.IsActive = isActive; await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);
        return await _db.SystemSettings.AsNoTracking().OrderBy(x => x.Group).ThenBy(x => x.Key)
            .Select(x => new SystemSettingDto(x.Id, x.Key, x.IsSensitive ? "••••••••" : x.Value,
                x.ValueType, x.Description, x.Group, x.IsPublic, x.IsSensitive, x.UpdatedByUserId, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemSettingDto> UpdateSettingAsync(string key, UpdateSystemSettingRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSettingsAsync(cancellationToken);
        var entity = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken)
            ?? throw new NotFoundException("System setting was not found.");
        if (entity.IsSensitive && request.Value == "••••••••")
            throw new BusinessRuleViolationException("Enter a new value for a sensitive setting.");
        ValidateSettingValue(entity.ValueType, request.Value);
        entity.Value = request.Value.Trim(); entity.Description = Clean(request.Description) ?? entity.Description;
        if (request.IsPublic.HasValue && !entity.IsSensitive) entity.IsPublic = request.IsPublic.Value;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return new SystemSettingDto(entity.Id, entity.Key, entity.IsSensitive ? "••••••••" : entity.Value,
            entity.ValueType, entity.Description, entity.Group, entity.IsPublic, entity.IsSensitive, entity.UpdatedByUserId, entity.UpdatedAt);
    }

    public async Task<AdminPagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        query.Page = Math.Max(1, query.Page); query.PageSize = Math.Clamp(query.PageSize, 1, 200);
        var source = ApplyAuditFilters(_db.AuditLogs.AsNoTracking(), query);
        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new AuditLogDto(x.Id, x.UserId, x.UserEmail, x.UserRole, x.Action, x.EntityName,
                x.EntityId, x.IpAddress, x.UserAgent, x.CorrelationId, x.Severity, x.Success,
                x.HttpStatusCode, x.ErrorMessage, x.NewValuesJson, x.CreatedAt)).ToListAsync(cancellationToken);
        return new AdminPagedResult<AuditLogDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<byte[]> ExportAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        query.Page = 1; query.PageSize = 5000;
        var data = await GetAuditLogsAsync(query, cancellationToken);
        var csv = new StringBuilder("CreatedAt,User,Role,Action,Entity,EntityId,Success,HttpStatus,CorrelationId,IP\r\n");
        foreach (var x in data.Items)
            csv.AppendLine(string.Join(',', Csv(x.CreatedAt), Csv(x.UserEmail), Csv(x.UserRole), Csv(x.Action),
                Csv(x.EntityName), Csv(x.EntityId), Csv(x.Success), Csv(x.HttpStatusCode), Csv(x.CorrelationId), Csv(x.IpAddress)));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var report = await _healthChecks.CheckHealthAsync(cancellationToken);
        var dependencies = report.Entries.Select(x => new DependencyHealthDto(x.Key, x.Value.Status.ToString(),
            x.Value.Description, Math.Round(x.Value.Duration.TotalMilliseconds, 2))).OrderBy(x => x.Name).ToArray();
        var uptime = DateTimeOffset.UtcNow - new DateTimeOffset(Process.GetCurrentProcess().StartTime.ToUniversalTime());
        return new SystemHealthDto(report.Status.ToString(), _environment.EnvironmentName,
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            DateTimeOffset.UtcNow, Math.Max(0, (long)uptime.TotalSeconds), GC.GetTotalMemory(false) / 1024 / 1024, dependencies);
    }

    public async Task<MaintenancePreviewDto> CleanupAsync(bool dryRun, int retentionDays, CancellationToken cancellationToken = default)
    {
        retentionDays = Math.Clamp(retentionDays, 30, 3650);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var notifications = await _db.Notifications.CountAsync(x => x.IsRead && x.IsArchived && x.CreatedAt < cutoff, cancellationToken);
        var refreshTokens = await _db.Users.CountAsync(x => x.RefreshTokenExpiresAt != null && x.RefreshTokenExpiresAt < DateTimeOffset.UtcNow && x.RefreshTokenHash != null, cancellationToken);
        var emailTokens = await _db.Users.CountAsync(x => x.EmailVerificationTokenExpiresAt != null && x.EmailVerificationTokenExpiresAt < DateTimeOffset.UtcNow && x.EmailVerificationTokenHash != null, cancellationToken);
        if (!dryRun)
        {
            var oldNotifications = await _db.Notifications.Where(x => x.IsRead && x.IsArchived && x.CreatedAt < cutoff).ToListAsync(cancellationToken);
            _db.Notifications.RemoveRange(oldNotifications);
            var users = await _db.Users.Where(x => (x.RefreshTokenExpiresAt != null && x.RefreshTokenExpiresAt < DateTimeOffset.UtcNow && x.RefreshTokenHash != null) ||
                (x.EmailVerificationTokenExpiresAt != null && x.EmailVerificationTokenExpiresAt < DateTimeOffset.UtcNow && x.EmailVerificationTokenHash != null)).ToListAsync(cancellationToken);
            foreach (var user in users)
            {
                if (user.RefreshTokenExpiresAt < DateTimeOffset.UtcNow) { user.RefreshTokenHash = null; user.RefreshTokenCreatedAt = null; user.RefreshTokenExpiresAt = null; }
                if (user.EmailVerificationTokenExpiresAt < DateTimeOffset.UtcNow) { user.EmailVerificationTokenHash = null; user.EmailVerificationTokenExpiresAt = null; }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        return new MaintenancePreviewDto(notifications, refreshTokens, emailTokens, retentionDays, dryRun, DateTimeOffset.UtcNow);
    }

    private async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _db.ComplaintCategories.IgnoreQueryFilters().AnyAsync(x => !x.IsDeleted, cancellationToken)) return;
        _db.ComplaintCategories.AddRange(
            new ComplaintCategoryEntity { Name = "Roads & Potholes", Code = "ROADS", Description = "Potholes, damaged roads and footpaths.", DefaultPriority = "High", Icon = "road", SortOrder = 10 },
            new ComplaintCategoryEntity { Name = "Garbage & Sanitation", Code = "SANITATION", Description = "Garbage collection, dumping and sanitation.", DefaultPriority = "Medium", Icon = "trash", SortOrder = 20 },
            new ComplaintCategoryEntity { Name = "Streetlights", Code = "STREETLIGHT", Description = "Broken or unsafe public lighting.", DefaultPriority = "Medium", Icon = "lightbulb", SortOrder = 30 },
            new ComplaintCategoryEntity { Name = "Water & Drainage", Code = "WATER", Description = "Leakage, drainage and water supply issues.", DefaultPriority = "High", Icon = "droplet", SortOrder = 40 },
            new ComplaintCategoryEntity { Name = "Public Safety", Code = "SAFETY", Description = "Urgent hazards in public areas.", DefaultPriority = "Critical", Icon = "shield", SortOrder = 50 },
            new ComplaintCategoryEntity { Name = "Other", Code = "OTHER", Description = "Other municipal civic issues.", DefaultPriority = "Medium", Icon = "circle", SortOrder = 100 });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDefaultSettingsAsync(CancellationToken cancellationToken)
    {
        if (await _db.SystemSettings.AnyAsync(cancellationToken)) return;
        _db.SystemSettings.AddRange(
            Setting("Complaints.DailySubmissionLimit", "10", "Integer", "Maximum citizen complaints per day.", "Complaints", true),
            Setting("Complaints.NearbyRadiusKm", "5", "Decimal", "Default nearby complaint radius.", "Complaints", true),
            Setting("Verification.GeoFenceMeters", "500", "Integer", "Citizen verification geo-fence radius.", "Verification", true),
            Setting("Rewards.ClosurePoints", "10", "Integer", "Points awarded after valid closure.", "Rewards", true),
            Setting("Notifications.RetentionDays", "365", "Integer", "Archived notification retention period.", "Maintenance", false),
            Setting("Platform.MaintenanceMode", "false", "Boolean", "Blocks non-admin writes when enabled by a future release.", "Platform", true),
            Setting("Platform.SupportEmail", "support@civichero.local", "String", "Support contact displayed to users.", "Platform", true));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static SystemSetting Setting(string key, string value, string type, string description, string group, bool isPublic) =>
        new() { Key = key, Value = value, ValueType = type, Description = description, Group = group, IsPublic = isPublic };

    private async Task EnsureDepartmentAsync(long? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return;
        if (!await _db.Departments.AnyAsync(x => x.Id == id.Value && x.IsActive, cancellationToken))
            throw new BusinessRuleViolationException("Selected department is unavailable.");
    }

    private static void ValidateCategory(SaveCategoryRequest request)
    {
        ValidateNameCode(request.Name, request.Code);
        if (!PriorityNames.Contains(request.DefaultPriority, StringComparer.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("Default priority must be Low, Medium, High or Critical.");
        if (request.SortOrder is < 0 or > 10000) throw new BusinessRuleViolationException("Sort order is outside the allowed range.");
    }
    private static void ValidateWard(SaveWardRequest request)
    {
        ValidateNameCode(request.Name, request.Code);
        if (request.DepartmentId <= 0) throw new BusinessRuleViolationException("Department is required.");
        if (request.BoundaryNorth < request.BoundarySouth || request.BoundaryEast < request.BoundaryWest)
            throw new BusinessRuleViolationException("Ward boundaries are invalid.");
        if (request.BoundaryNorth is > 90 or < -90 || request.BoundarySouth is > 90 or < -90 || request.BoundaryEast is > 180 or < -180 || request.BoundaryWest is > 180 or < -180)
            throw new BusinessRuleViolationException("Ward boundaries must contain valid GPS coordinates.");
    }
    private static void ValidateNameCode(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150) throw new BusinessRuleViolationException("A valid name is required.");
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 40) throw new BusinessRuleViolationException("A valid code is required.");
    }
    private static void ValidateSettingValue(string type, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new BusinessRuleViolationException("Setting value is required.");
        if (type.Equals("Integer", StringComparison.OrdinalIgnoreCase) && !int.TryParse(value, out _)) throw new BusinessRuleViolationException("Setting requires an integer value.");
        if (type.Equals("Decimal", StringComparison.OrdinalIgnoreCase) && !decimal.TryParse(value, out _)) throw new BusinessRuleViolationException("Setting requires a decimal value.");
        if (type.Equals("Boolean", StringComparison.OrdinalIgnoreCase) && !bool.TryParse(value, out _)) throw new BusinessRuleViolationException("Setting requires true or false.");
    }
    private static IQueryable<AuditLog> ApplyAuditFilters(IQueryable<AuditLog> source, AuditLogQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search)) { var s = query.Search.Trim(); source = source.Where(x => (x.UserEmail != null && x.UserEmail.Contains(s)) || x.Action.Contains(s) || x.EntityName.Contains(s) || (x.EntityId != null && x.EntityId.Contains(s)) || (x.CorrelationId != null && x.CorrelationId.Contains(s))); }
        if (!string.IsNullOrWhiteSpace(query.Action)) source = source.Where(x => x.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityName)) source = source.Where(x => x.EntityName == query.EntityName);
        if (!string.IsNullOrWhiteSpace(query.UserRole)) source = source.Where(x => x.UserRole == query.UserRole);
        if (query.Success.HasValue) source = source.Where(x => x.Success == query.Success.Value);
        if (query.From.HasValue) source = source.Where(x => x.CreatedAt >= query.From.Value);
        if (query.To.HasValue) source = source.Where(x => x.CreatedAt <= query.To.Value);
        return source;
    }
    private static HashSet<ComplaintStatus> OpenComplaintStatuses() => Enum.GetValues<ComplaintStatus>()
        .Where(x => x is not ComplaintStatus.Closed and not ComplaintStatus.ClosedAuto and not ComplaintStatus.ClosedFraud and not ComplaintStatus.Withdrawn and not ComplaintStatus.Merged).ToHashSet();
    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant().Replace(' ', '-');
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Csv(object? value) { var text = value?.ToString() ?? string.Empty; return $"\"{text.Replace("\"", "\"\"")}\""; }
}
