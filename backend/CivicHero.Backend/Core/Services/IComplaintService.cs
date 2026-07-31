using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Infrastructure.Storage;

namespace CivicHero.Backend.Core.Services;

public interface IComplaintService
{
    Task<ComplaintDetailResponse> CreateAsync(CreateComplaintRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ComplaintResponse>> GetAsync(ComplaintQuery query, CancellationToken cancellationToken = default);
    Task<PagedResponse<ComplaintResponse>> GetPublicAsync(ComplaintQuery query, CancellationToken cancellationToken = default);
    Task<PagedResponse<ComplaintResponse>> GetMineAsync(ComplaintQuery query, CancellationToken cancellationToken = default);
    Task<ComplaintDetailResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ComplaintDetailResponse> UpdateAsync(long id, UpdateComplaintRequest request, CancellationToken cancellationToken = default);
    Task<ComplaintDetailResponse> WithdrawAsync(long id, CancellationToken cancellationToken = default);
    Task<int> UpvoteAsync(long id, CancellationToken cancellationToken = default);
    Task<int> RemoveUpvoteAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplaintResponse>> GetNearbyAsync(NearbyComplaintQuery query, CancellationToken cancellationToken = default);
    Task<ComplaintMetadataResponse> GetMetadataAsync(CancellationToken cancellationToken = default);
    Task<ComplaintDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<StorageDownload> DownloadImageAsync(long complaintId, long imageId, CancellationToken cancellationToken = default);
}
