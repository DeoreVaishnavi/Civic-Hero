using CivicHero.Backend.Core.DTOs.Administration;

namespace CivicHero.Backend.Core.Services;

public interface IAdminUserManagementService
{
    Task<AdminPagedResult<AdminManagedUserDto>> GetUsersAsync(AdminUserManagementQuery query, CancellationToken cancellationToken = default);
    Task<AdminCreateCitizenResponse> CreateCitizenAsync(AdminCreateCitizenRequest request, CancellationToken cancellationToken = default);
    Task<AdminUpdateUserEmailResponse> UpdateEmailAsync(long userId, AdminUpdateUserEmailRequest request, CancellationToken cancellationToken = default);
    Task<AdminManagedUserDto> SoftDeleteAsync(long userId, AdminUserLifecycleRequest request, CancellationToken cancellationToken = default);
    Task<AdminManagedUserDto> RestoreAsync(long userId, AdminUserLifecycleRequest request, CancellationToken cancellationToken = default);
    Task<AdminUserHistoryResponse> GetHistoryAsync(long userId, int page, int pageSize, bool roleChangesOnly, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentHeadDto>> GetDepartmentHeadsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentHeadDto> AssignDepartmentHeadAsync(long departmentId, DepartmentHeadAssignmentRequest request, CancellationToken cancellationToken = default);
    Task RemoveDepartmentHeadAsync(long departmentId, DepartmentHeadRemovalRequest request, CancellationToken cancellationToken = default);
    Task<AdminMasterDataConfigurationDto> GetMasterDataAsync(CancellationToken cancellationToken = default);
    Task<AdminMasterDataConfigurationDto> UpdateMasterDataAsync(AdminMasterDataConfigurationDto request, CancellationToken cancellationToken = default);
}
