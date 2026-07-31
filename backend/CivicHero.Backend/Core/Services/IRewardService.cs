using CivicHero.Backend.Core.DTOs.Rewards;

namespace CivicHero.Backend.Core.Services;

public interface IRewardService
{
    Task<PointsBalanceResponse> GetPointsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaderboardEntryResponse>> GetLeaderboardAsync(int limit, CancellationToken cancellationToken = default);
    Task<LeaderboardCitizenProfileResponse> GetLeaderboardCitizenProfileAsync(long targetUserId, CancellationToken cancellationToken = default);
    Task<LeaderboardCitizenProfileResponse> FollowCitizenAsync(long targetUserId, CancellationToken cancellationToken = default);
    Task<LeaderboardCitizenProfileResponse> UnfollowCitizenAsync(long targetUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BadgeResponse>> GetBadgesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RewardCatalogResponse>> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<RedemptionResponse> RedeemAsync(RedeemRewardRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RedemptionResponse>> GetRedemptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PointsHistoryResponse>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<(byte[] Content, string FileName)> GetCertificateAsync(CancellationToken cancellationToken = default);
    Task<int> ProcessEligibleAwardsAsync(CancellationToken cancellationToken = default);
}
