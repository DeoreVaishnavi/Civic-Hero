using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Services;

public interface IRewardService
{
    Task<PointsBalanceResponse> GetPointsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaderboardEntryResponse>> GetLeaderboardAsync(int limit, LeaderboardTimeframe timeframe = LeaderboardTimeframe.AllTime, CancellationToken cancellationToken = default);
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

    Task<IReadOnlyList<AdminRewardCatalogResponse>> GetAdminCatalogAsync(CancellationToken cancellationToken = default);
    Task<AdminRewardCatalogResponse> CreateRewardAsync(SaveRewardCatalogRequest request, CancellationToken cancellationToken = default);
    Task<AdminRewardCatalogResponse> UpdateRewardAsync(long id, SaveRewardCatalogRequest request, CancellationToken cancellationToken = default);
    Task<AdminRewardCatalogResponse> RefillRewardStockAsync(long id, AdjustRewardStockRequest request, CancellationToken cancellationToken = default);
    Task<AdminRewardCatalogResponse> SetRewardActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<RewardRulesResponse> GetRewardRulesAsync(CancellationToken cancellationToken = default);
    Task<RewardRulesResponse> SaveBadgeRulesAsync(SaveBadgeRulesRequest request, CancellationToken cancellationToken = default);
    Task<RewardRulesResponse> SaveTierRulesAsync(SaveTierRulesRequest request, CancellationToken cancellationToken = default);
    Task<ManualPointsAdjustmentResponse> AdjustPointsAsync(ManualPointsAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminRedemptionResponse>> GetAdminRedemptionsAsync(AdminRedemptionQuery query, CancellationToken cancellationToken = default);
    Task<AdminRedemptionResponse> UpdateRedemptionStatusAsync(long id, UpdateRedemptionStatusRequest request, CancellationToken cancellationToken = default);
}
