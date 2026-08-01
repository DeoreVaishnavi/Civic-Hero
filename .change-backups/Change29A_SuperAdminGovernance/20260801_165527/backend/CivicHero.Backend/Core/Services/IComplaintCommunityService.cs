using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Complaints;

namespace CivicHero.Backend.Core.Services;

public interface IComplaintCommunityService
{
    Task<ComplaintFollowStatusResponse> FollowAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<ComplaintFollowStatusResponse> UnfollowAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<ComplaintFollowStatusResponse> GetFollowStatusAsync(long complaintId, CancellationToken cancellationToken = default);
    Task<ComplaintFollowingIdsResponse> GetFollowingIdsAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<FollowedComplaintResponse>> GetFollowingAsync(ComplaintQuery query, CancellationToken cancellationToken = default);
    Task<int> NotifyFollowersAsync(
        long complaintId,
        string title,
        string message,
        long? actorUserId = null,
        IReadOnlyCollection<long>? excludedUserIds = null,
        CancellationToken cancellationToken = default);
}
