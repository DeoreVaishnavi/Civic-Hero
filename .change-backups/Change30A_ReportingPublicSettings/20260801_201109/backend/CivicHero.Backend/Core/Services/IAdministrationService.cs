using CivicHero.Backend.Core.DTOs.Administration;

namespace CivicHero.Backend.Core.Services;

public interface IAdministrationService
{
    Task<AdministrationOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(long id, SaveCategoryRequest request, CancellationToken cancellationToken = default);
    Task SetCategoryActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentAdminDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentAdminDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentAdminDto> UpdateDepartmentAsync(long id, SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task SetDepartmentActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WardAdminDto>> GetWardsAsync(long? departmentId, CancellationToken cancellationToken = default);
    Task<WardAdminDto> CreateWardAsync(SaveWardRequest request, CancellationToken cancellationToken = default);
    Task<WardAdminDto> UpdateWardAsync(long id, SaveWardRequest request, CancellationToken cancellationToken = default);
    Task SetWardActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemSettingDto>> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingDto> UpdateSettingAsync(string key, UpdateSystemSettingRequest request, CancellationToken cancellationToken = default);
    Task<AdminPagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
    Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    Task<MaintenancePreviewDto> CleanupAsync(bool dryRun, int retentionDays, CancellationToken cancellationToken = default);
}
