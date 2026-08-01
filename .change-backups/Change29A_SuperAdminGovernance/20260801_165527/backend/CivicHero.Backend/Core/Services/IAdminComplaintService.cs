using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.DTOs.Common;

namespace CivicHero.Backend.Core.Services;

public interface IAdminComplaintService
{
    Task<PagedResponse<AdminComplaintItemResponse>> GetAsync(AdminComplaintQuery query, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> ChangePriorityAsync(long id, AdminComplaintPriorityRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> CorrectRoutingAsync(long id, AdminComplaintRoutingRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> OverrideAssignmentAsync(long id, AdminComplaintAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> CloseAsync(long id, AdminComplaintActionRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> ReopenAsync(long id, AdminComplaintActionRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> ArchiveAsync(long id, AdminComplaintActionRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> RestoreAsync(long id, AdminComplaintActionRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> LinkDuplicateAsync(long id, AdminComplaintDuplicateRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> MergeAsync(long id, AdminComplaintDuplicateRequest request, CancellationToken cancellationToken = default);
    Task<AdminComplaintDetailResponse> RemoveMediaAsync(long id, long mediaId, AdminComplaintActionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplicateClusterResponse>> GetDuplicateClustersAsync(bool includeArchived, CancellationToken cancellationToken = default);
}
