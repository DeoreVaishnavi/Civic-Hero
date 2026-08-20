using System.Text;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class RewardService : IRewardService
{
    private const string ComplaintReference = "Complaint";
    private const string ClosedRewardReason = "Valid complaint closed";
    private const string AutoClosedRewardReason = "Complaint auto-closed after verification window";
    private const string CitizenProfileEntity = "CitizenProfile";
    private const string CitizenFollowedAction = "CitizenFollowed";
    private const string CitizenUnfollowedAction = "CitizenUnfollowed";
    private const string BadgeRulesSettingKey = "Rewards.BadgeRules";
    private const string TierRulesSettingKey = "Rewards.TierRules";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyList<BadgeRuleDto> DefaultBadgeRules =
    [
        new() { Code = "FIRST_FIX", Name = "First Fix", Description = "Close your first valid civic complaint.", Icon = "✓", Metric = "ClosedComplaints", Target = 1, IsActive = true, DisplayOrder = 10 },
        new() { Code = "NEIGHBORHOOD_GUARDIAN", Name = "Neighborhood Guardian", Description = "Help close five civic complaints.", Icon = "◆", Metric = "ClosedComplaints", Target = 5, IsActive = true, DisplayOrder = 20 },
        new() { Code = "TRUSTED_VERIFIER", Name = "Trusted Verifier", Description = "Approve five genuine resolutions.", Icon = "◎", Metric = "ApprovedVerifications", Target = 5, IsActive = true, DisplayOrder = 30 },
        new() { Code = "CIVIC_CHAMPION", Name = "Civic Champion", Description = "Earn 1,000 CivicHero points.", Icon = "★", Metric = "Points", Target = 1000, IsActive = true, DisplayOrder = 40 }
    ];

    private static readonly IReadOnlyList<TierRuleDto> DefaultTierRules =
    [
        new() { Name = "New Citizen", MinimumPoints = 0 },
        new() { Name = "Contributor", MinimumPoints = 100 },
        new() { Name = "Neighborhood Guardian", MinimumPoints = 500 },
        new() { Name = "Civic Champion", MinimumPoints = 1000 },
        new() { Name = "City Champion", MinimumPoints = 2000 }
    ];

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly RewardsOptions _options;

    public RewardService(CivicDbContext db, ICurrentUserService currentUser, INotificationService notifications, IOptions<RewardsOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _options = options.Value;
    }

    public async Task<PointsBalanceResponse> GetPointsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        var logs = await _db.ReputationLogs.AsNoTracking().Where(entity => entity.UserId == userId).ToListAsync(cancellationToken);
        var balance = logs.Sum(entity => entity.PointsDelta);
        var rank = await CalculateRankAsync(userId, cancellationToken);
        var tier = ResolveTier(balance, await GetTierRulesInternalAsync(cancellationToken));
        return new PointsBalanceResponse
        {
            Balance = balance,
            LifetimeEarned = logs.Where(entity => entity.PointsDelta > 0).Sum(entity => entity.PointsDelta),
            LifetimeSpent = Math.Abs(logs.Where(entity => entity.PointsDelta < 0).Sum(entity => entity.PointsDelta)),
            Rank = rank,
            Tier = tier.Tier,
            NextTierAt = tier.NextTier
        };
    }

    public async Task<IReadOnlyList<LeaderboardEntryResponse>> GetLeaderboardAsync(
        int limit,
        LeaderboardTimeframe timeframe = LeaderboardTimeframe.AllTime,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 5, 100);
        var currentUserId = _currentUser.UserId;
        var periodStart = LeaderboardPeriodStart(timeframe, DateTimeOffset.UtcNow);

        IQueryable<ReputationLog> reputationQuery = _db.ReputationLogs.AsNoTracking();
        if (periodStart.HasValue)
            reputationQuery = reputationQuery.Where(entity => entity.CreatedAt >= periodStart.Value);

        var totals = await reputationQuery
            .GroupBy(entity => entity.UserId)
            .Select(group => new { UserId = group.Key, Points = group.Sum(entity => entity.PointsDelta) })
            .Where(item => item.Points > 0)
            .OrderByDescending(item => item.Points)
            .ThenBy(item => item.UserId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var ids = totals.Select(item => item.UserId).ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(entity => ids.Contains(entity.Id) && entity.Role == UserRole.Citizen && entity.IsActive && !entity.IsDeleted)
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);

        IQueryable<Complaint> complaintQuery = _db.Complaints.AsNoTracking()
            .Where(entity => ids.Contains(entity.CitizenId) &&
                (entity.Status == ComplaintStatus.Closed || entity.Status == ComplaintStatus.ClosedAuto));
        if (periodStart.HasValue)
            complaintQuery = complaintQuery.Where(entity => entity.ClosedAt.HasValue && entity.ClosedAt.Value >= periodStart.Value);

        var closedCounts = await complaintQuery
            .GroupBy(entity => entity.CitizenId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count, cancellationToken);

        var tierRules = await GetTierRulesInternalAsync(cancellationToken);
        return totals
            .Where(item => users.ContainsKey(item.UserId))
            .Select((item, index) => new LeaderboardEntryResponse
            {
                Rank = index + 1,
                UserId = item.UserId,
                CitizenName = MaskName(users[item.UserId].FullName),
                Points = item.Points,
                ClosedComplaints = closedCounts.GetValueOrDefault(item.UserId),
                Tier = ResolveTier(item.Points, tierRules).Tier,
                IsCurrentUser = currentUserId == item.UserId
            })
            .ToList();
    }

    public async Task<LeaderboardCitizenProfileResponse> GetLeaderboardCitizenProfileAsync(long targetUserId, CancellationToken cancellationToken = default)
    {
        var currentUserId = RequireCitizenId();
        return await BuildLeaderboardCitizenProfileAsync(currentUserId, targetUserId, cancellationToken);
    }

    public async Task<LeaderboardCitizenProfileResponse> FollowCitizenAsync(long targetUserId, CancellationToken cancellationToken = default)
    {
        var currentUserId = RequireCitizenId();
        if (currentUserId == targetUserId)
            throw new BusinessRuleViolationException("You cannot follow your own profile.");

        await EnsureLeaderboardCitizenExistsAsync(targetUserId, cancellationToken);
        var followState = await GetFollowStateAsync(targetUserId, currentUserId, cancellationToken);
        if (!followState.IsFollowing)
        {
            _db.AuditLogs.Add(CreateCitizenFollowAudit(currentUserId, targetUserId, CitizenFollowedAction));
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await BuildLeaderboardCitizenProfileAsync(currentUserId, targetUserId, cancellationToken);
    }

    public async Task<LeaderboardCitizenProfileResponse> UnfollowCitizenAsync(long targetUserId, CancellationToken cancellationToken = default)
    {
        var currentUserId = RequireCitizenId();
        if (currentUserId == targetUserId)
            throw new BusinessRuleViolationException("You cannot unfollow your own profile.");

        await EnsureLeaderboardCitizenExistsAsync(targetUserId, cancellationToken);
        var followState = await GetFollowStateAsync(targetUserId, currentUserId, cancellationToken);
        if (followState.IsFollowing)
        {
            _db.AuditLogs.Add(CreateCitizenFollowAudit(currentUserId, targetUserId, CitizenUnfollowedAction));
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await BuildLeaderboardCitizenProfileAsync(currentUserId, targetUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<BadgeResponse>> GetBadgesAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        return await BuildBadgeResponsesAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<RewardCatalogResponse>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        var balance = await BalanceAsync(userId, cancellationToken);
        var rewards = await _db.RewardCatalog.AsNoTracking()
            .Where(entity => entity.IsActive && entity.StockQuantity > 0)
            .OrderBy(entity => entity.PointsCost)
            .ToListAsync(cancellationToken);

        return rewards.Select(entity => new RewardCatalogResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            PointsCost = entity.PointsCost,
            Type = entity.Type.ToString(),
            StockQuantity = entity.StockQuantity,
            CanRedeem = balance >= entity.PointsCost
        }).ToList();
    }

    public async Task<RedemptionResponse> RedeemAsync(RedeemRewardRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var reward = await _db.RewardCatalog.FirstOrDefaultAsync(entity => entity.Id == request.RewardCatalogId && entity.IsActive && entity.StockQuantity > 0, cancellationToken)
            ?? throw new NotFoundException("Reward is unavailable.");
        var balance = await BalanceAsync(userId, cancellationToken);
        if (balance < reward.PointsCost)
            throw new BusinessRuleViolationException("You do not have enough CivicHero points for this reward.");

        reward.StockQuantity--;
        var now = DateTimeOffset.UtcNow;
        var immediateFulfilment = reward.Type is RewardType.Certificate or RewardType.Badge;
        var redemption = new Redemption
        {
            UserId = userId,
            RewardCatalogId = reward.Id,
            PointsSpent = reward.PointsCost,
            Status = immediateFulfilment ? RedemptionStatus.Fulfilled : RedemptionStatus.Pending,
            RedemptionCode = $"CHR-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CreatedAt = now,
            FulfilledAt = immediateFulfilment ? now : null
        };
        _db.Redemptions.Add(redemption);
        await _db.SaveChangesAsync(cancellationToken);
        _db.ReputationLogs.Add(new ReputationLog
        {
            UserId = userId,
            PointsDelta = -reward.PointsCost,
            RunningBalance = balance - reward.PointsCost,
            Reason = $"Redeemed: {reward.Name}",
            ReferenceType = "Redemption",
            ReferenceId = redemption.Id,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _notifications.SendAsync(new NotificationDispatchRequest(userId, "Reward redeemed", $"{reward.Name} was redeemed successfully. Code: {redemption.RedemptionCode}", nameof(NotificationType.RewardEarned), "Redemption", redemption.Id, "/citizen/rewards"), cancellationToken);
        return Map(redemption, reward.Name);
    }

    public async Task<IReadOnlyList<RedemptionResponse>> GetRedemptionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        var redemptions = await _db.Redemptions.AsNoTracking()
            .Include(entity => entity.RewardCatalog)
            .Where(entity => entity.UserId == userId)
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);

        return redemptions.Select(entity => Map(entity, entity.RewardCatalog.Name)).ToList();
    }

    public async Task<IReadOnlyList<PointsHistoryResponse>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        return await _db.ReputationLogs.AsNoTracking()
            .Where(entity => entity.UserId == userId)
            .OrderByDescending(entity => entity.CreatedAt)
            .Take(200)
            .Select(entity => new PointsHistoryResponse
            {
                Id = entity.Id,
                PointsDelta = entity.PointsDelta,
                RunningBalance = entity.RunningBalance,
                Reason = entity.Reason,
                ReferenceType = entity.ReferenceType,
                ReferenceId = entity.ReferenceId,
                CreatedAt = entity.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(byte[] Content, string FileName)> GetCertificateAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireCitizenId();
        var user = await _db.Users.AsNoTracking().FirstAsync(entity => entity.Id == userId, cancellationToken);
        var points = await BalanceAsync(userId, cancellationToken);
        var closed = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == userId && (entity.Status == ComplaintStatus.Closed || entity.Status == ComplaintStatus.ClosedAuto), cancellationToken);
        if (points < 250)
            throw new BusinessRuleViolationException("Earn at least 250 points before downloading a certificate.");

        var html = $$"""
        <!doctype html><html><head><meta charset="utf-8"><title>CivicHero Certificate</title>
        <style>body{font-family:Arial,sans-serif;background:#07111f;color:#10233f;padding:40px}.certificate{max-width:900px;margin:auto;background:white;border:12px solid #0ea5e9;padding:70px;text-align:center}h1{font-size:48px;margin:0;color:#0369a1}h2{font-size:34px}p{font-size:20px;line-height:1.6}.seal{font-size:64px}small{color:#475569}</style></head>
        <body><main class="certificate"><div class="seal">★</div><h1>CivicHero</h1><p>Certificate of Civic Contribution</p><h2>{{System.Net.WebUtility.HtmlEncode(user.FullName)}}</h2>
        <p>is recognised for responsible civic participation, helping close <strong>{{closed}}</strong> civic issue(s) and earning <strong>{{points}}</strong> CivicHero points.</p>
        <small>Issued {{DateTimeOffset.UtcNow:dd MMMM yyyy}} · Certificate CH-{{userId:D6}}-{{points:D6}}</small></main></body></html>
        """;
        return (Encoding.UTF8.GetBytes(html), $"CivicHero-Certificate-{userId}.html");
    }

    public async Task<int> ProcessEligibleAwardsAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _db.Complaints.IgnoreQueryFilters().AsNoTracking()
            .Where(entity => !entity.IsDeleted && (entity.Status == ComplaintStatus.Closed || entity.Status == ComplaintStatus.ClosedAuto))
            .Where(entity => !entity.IsAnonymous && !_db.ReputationLogs.Any(log => log.UserId == entity.CitizenId && log.ReferenceType == ComplaintReference && log.ReferenceId == entity.Id && (log.Reason == ClosedRewardReason || log.Reason == AutoClosedRewardReason)))
            .OrderBy(entity => entity.ClosedAt)
            .Take(100)
            .Select(entity => new { entity.Id, entity.CitizenId, entity.Status })
            .ToListAsync(cancellationToken);

        var awarded = 0;
        foreach (var complaint in candidates)
        {
            var basePoints = complaint.Status == ComplaintStatus.ClosedAuto ? _options.AutoClosedComplaintPoints : _options.ClosedComplaintPoints;
            var reason = complaint.Status == ComplaintStatus.ClosedAuto ? AutoClosedRewardReason : ClosedRewardReason;
            var hasApprovedVerification = await _db.ComplaintVerifications.AsNoTracking().AnyAsync(entity => entity.ComplaintId == complaint.Id && entity.Decision == VerificationDecision.Approved, cancellationToken);
            var upvotes = await _db.ComplaintVotes.AsNoTracking().CountAsync(entity => entity.ComplaintId == complaint.Id, cancellationToken);
            var points = basePoints + (hasApprovedVerification ? _options.VerificationBonusPoints : 0) + (upvotes * _options.UpvoteBonusPerVote);
            var balance = await BalanceAsync(complaint.CitizenId, cancellationToken);
            _db.ReputationLogs.Add(new ReputationLog
            {
                UserId = complaint.CitizenId,
                PointsDelta = points,
                RunningBalance = balance + points,
                Reason = reason,
                ReferenceType = ComplaintReference,
                ReferenceId = complaint.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                awarded++;
                await _notifications.SendAsync(new NotificationDispatchRequest(complaint.CitizenId, "CivicHero points earned", $"You earned {points} points after valid closure, verification and community support were calculated.", nameof(NotificationType.RewardEarned), ComplaintReference, complaint.Id, "/citizen/rewards"), cancellationToken);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
            }
        }

        return awarded;
    }

    public async Task<IReadOnlyList<AdminRewardCatalogResponse>> GetAdminCatalogAsync(CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        return await _db.RewardCatalog.AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => new AdminRewardCatalogResponse
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                PointsCost = entity.PointsCost,
                Type = entity.Type.ToString(),
                StockQuantity = entity.StockQuantity,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminRewardCatalogResponse> CreateRewardAsync(SaveRewardCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var name = request.Name.Trim();
        if (await _db.RewardCatalog.AnyAsync(entity => entity.Name == name, cancellationToken))
            throw new BusinessRuleViolationException("A reward with this name already exists.");

        var entity = new RewardCatalog
        {
            Name = name,
            Description = request.Description.Trim(),
            PointsCost = request.PointsCost,
            Type = ParseRewardType(request.Type),
            StockQuantity = request.StockQuantity,
            IsActive = request.IsActive
        };
        _db.RewardCatalog.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "RewardCreated", "RewardCatalog", entity.Id.ToString(), null, MapAdminReward(entity)));
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdminReward(entity);
    }

    public async Task<AdminRewardCatalogResponse> UpdateRewardAsync(long id, SaveRewardCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var entity = await _db.RewardCatalog.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Reward was not found.");
        var name = request.Name.Trim();
        if (await _db.RewardCatalog.AnyAsync(item => item.Id != id && item.Name == name, cancellationToken))
            throw new BusinessRuleViolationException("A reward with this name already exists.");

        var oldValue = MapAdminReward(entity);
        entity.Name = name;
        entity.Description = request.Description.Trim();
        entity.PointsCost = request.PointsCost;
        entity.Type = ParseRewardType(request.Type);
        entity.StockQuantity = request.StockQuantity;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        var updated = MapAdminReward(entity);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "RewardUpdated", "RewardCatalog", entity.Id.ToString(), oldValue, updated));
        await _db.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<AdminRewardCatalogResponse> RefillRewardStockAsync(long id, AdjustRewardStockRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var entity = await _db.RewardCatalog.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Reward was not found.");
        var previousStock = entity.StockQuantity;
        if ((long)entity.StockQuantity + request.Quantity > 1_000_000)
            throw new BusinessRuleViolationException("Reward stock cannot exceed 1,000,000 units.");
        entity.StockQuantity += request.Quantity;
        await _db.SaveChangesAsync(cancellationToken);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "RewardStockRefilled", "RewardCatalog", id.ToString(), new { stockQuantity = previousStock }, new { entity.StockQuantity, reason = request.Reason.Trim() }));
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdminReward(entity);
    }

    public async Task<AdminRewardCatalogResponse> SetRewardActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var entity = await _db.RewardCatalog.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Reward was not found.");
        var oldValue = entity.IsActive;
        entity.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, isActive ? "RewardActivated" : "RewardDeactivated", "RewardCatalog", id.ToString(), new { isActive = oldValue }, new { isActive }));
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdminReward(entity);
    }

    public async Task<RewardRulesResponse> GetRewardRulesAsync(CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        return new RewardRulesResponse
        {
            BadgeRules = await GetBadgeRulesInternalAsync(cancellationToken),
            TierRules = await GetTierRulesInternalAsync(cancellationToken)
        };
    }

    public async Task<RewardRulesResponse> SaveBadgeRulesAsync(SaveBadgeRulesRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var normalized = request.Rules
            .Select(rule => new BadgeRuleDto
            {
                Code = rule.Code.Trim().ToUpperInvariant(),
                Name = rule.Name.Trim(),
                Description = rule.Description.Trim(),
                Icon = rule.Icon.Trim(),
                Metric = CanonicalMetric(rule.Metric),
                Target = rule.Target,
                IsActive = rule.IsActive,
                DisplayOrder = rule.DisplayOrder
            })
            .OrderBy(rule => rule.DisplayOrder)
            .ThenBy(rule => rule.Name)
            .ToList();

        var oldRules = await GetBadgeRulesInternalAsync(cancellationToken);
        await UpsertRulesSettingAsync(BadgeRulesSettingKey, normalized, "Configurable CivicHero badge rules.", adminId, cancellationToken);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "BadgeRulesUpdated", "SystemSetting", BadgeRulesSettingKey, oldRules, normalized));
        await _db.SaveChangesAsync(cancellationToken);
        return new RewardRulesResponse { BadgeRules = normalized, TierRules = await GetTierRulesInternalAsync(cancellationToken) };
    }

    public async Task<RewardRulesResponse> SaveTierRulesAsync(SaveTierRulesRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var normalized = request.Rules
            .Select(rule => new TierRuleDto { Name = rule.Name.Trim(), MinimumPoints = rule.MinimumPoints })
            .OrderBy(rule => rule.MinimumPoints)
            .ToList();

        var oldRules = await GetTierRulesInternalAsync(cancellationToken);
        await UpsertRulesSettingAsync(TierRulesSettingKey, normalized, "Configurable CivicHero reputation tiers.", adminId, cancellationToken);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "TierRulesUpdated", "SystemSetting", TierRulesSettingKey, oldRules, normalized));
        await _db.SaveChangesAsync(cancellationToken);
        return new RewardRulesResponse { BadgeRules = await GetBadgeRulesInternalAsync(cancellationToken), TierRules = normalized };
    }

    public async Task<ManualPointsAdjustmentResponse> AdjustPointsAsync(ManualPointsAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var citizen = await _db.Users.FirstOrDefaultAsync(entity => entity.Id == request.UserId && entity.Role == UserRole.Citizen && entity.IsActive && !entity.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Active Citizen account was not found.");
        var balance = await BalanceAsync(citizen.Id, cancellationToken);
        var newBalance = balance + request.PointsDelta;
        if (newBalance < 0)
            throw new BusinessRuleViolationException("The adjustment cannot make the Citizen's points balance negative.");

        var now = DateTimeOffset.UtcNow;
        var reason = request.Reason.Trim();
        var audit = CreateAdminAudit(adminId, "CitizenPointsAdjusted", "User", citizen.Id.ToString(), new { balance }, new { pointsDelta = request.PointsDelta, newBalance, reason });
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync(cancellationToken);

        _db.ReputationLogs.Add(new ReputationLog
        {
            UserId = citizen.Id,
            PointsDelta = request.PointsDelta,
            RunningBalance = newBalance,
            Reason = $"Admin adjustment: {reason}",
            ReferenceType = "ManualRewardAdjustment",
            ReferenceId = audit.Id,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            citizen.Id,
            request.PointsDelta > 0 ? "CivicHero points added" : "CivicHero points adjusted",
            $"Your points balance was adjusted by {request.PointsDelta:+#;-#;0}. Reason: {reason}",
            nameof(NotificationType.RewardEarned),
            "ManualRewardAdjustment",
            audit.Id,
            "/citizen/rewards"), cancellationToken);

        return new ManualPointsAdjustmentResponse
        {
            UserId = citizen.Id,
            CitizenName = citizen.FullName,
            PointsDelta = request.PointsDelta,
            NewBalance = newBalance,
            Reason = reason,
            CreatedAt = now
        };
    }

    public async Task<IReadOnlyList<AdminRedemptionResponse>> GetAdminRedemptionsAsync(AdminRedemptionQuery query, CancellationToken cancellationToken = default)
    {
        RequireAdminId();
        var take = Math.Clamp(query.Take, 1, 500);
        IQueryable<Redemption> redemptions = _db.Redemptions.AsNoTracking()
            .Include(entity => entity.User)
            .Include(entity => entity.RewardCatalog);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<RedemptionStatus>(query.Status, true, out var status))
                throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Redemption status is invalid."]);
            redemptions = redemptions.Where(entity => entity.Status == status);
        }

        return await redemptions
            .OrderByDescending(entity => entity.CreatedAt)
            .Take(take)
            .Select(entity => new AdminRedemptionResponse
            {
                Id = entity.Id,
                UserId = entity.UserId,
                CitizenName = entity.User.FullName,
                CitizenEmail = entity.User.Email,
                RewardCatalogId = entity.RewardCatalogId,
                RewardName = entity.RewardCatalog.Name,
                PointsSpent = entity.PointsSpent,
                Status = entity.Status.ToString(),
                RedemptionCode = entity.RedemptionCode,
                CreatedAt = entity.CreatedAt,
                FulfilledAt = entity.FulfilledAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminRedemptionResponse> UpdateRedemptionStatusAsync(long id, UpdateRedemptionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var adminId = RequireAdminId();
        var targetStatus = ParseRedemptionStatus(request.Status);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await _db.Redemptions
            .Include(item => item.User)
            .Include(item => item.RewardCatalog)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Redemption was not found.");

        if (entity.Status == targetStatus)
            return MapAdminRedemption(entity);
        if (entity.Status is RedemptionStatus.Fulfilled or RedemptionStatus.Rejected or RedemptionStatus.Cancelled)
            throw new BusinessRuleViolationException("A final redemption decision cannot be changed.");
        if (entity.Status == RedemptionStatus.Pending && targetStatus == RedemptionStatus.Fulfilled && entity.RewardCatalog.Type is not (RewardType.Certificate or RewardType.Badge))
            throw new BusinessRuleViolationException("Approve a physical or voucher redemption before marking it fulfilled.");

        var oldStatus = entity.Status;
        var now = DateTimeOffset.UtcNow;
        var reason = request.Reason.Trim();

        if (targetStatus is RedemptionStatus.Rejected or RedemptionStatus.Cancelled)
        {
            var balance = await BalanceAsync(entity.UserId, cancellationToken);
            entity.RewardCatalog.StockQuantity++;
            _db.ReputationLogs.Add(new ReputationLog
            {
                UserId = entity.UserId,
                PointsDelta = entity.PointsSpent,
                RunningBalance = balance + entity.PointsSpent,
                Reason = $"Redemption refund: {entity.RewardCatalog.Name}",
                ReferenceType = "RedemptionRefund",
                ReferenceId = entity.Id,
                CreatedAt = now
            });
        }

        entity.Status = targetStatus;
        entity.FulfilledAt = targetStatus == RedemptionStatus.Fulfilled ? now : null;
        await _db.SaveChangesAsync(cancellationToken);
        _db.AuditLogs.Add(CreateAdminAudit(adminId, "RedemptionStatusUpdated", "Redemption", entity.Id.ToString(), new { status = oldStatus.ToString() }, new { status = targetStatus.ToString(), reason }));
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notifications.SendAsync(new NotificationDispatchRequest(
            entity.UserId,
            "Reward redemption updated",
            $"Your redemption for {entity.RewardCatalog.Name} is now {targetStatus}. Reason: {reason}",
            nameof(NotificationType.RewardEarned),
            "Redemption",
            entity.Id,
            "/citizen/rewards"), cancellationToken);

        return MapAdminRedemption(entity);
    }

    private async Task<LeaderboardCitizenProfileResponse> BuildLeaderboardCitizenProfileAsync(long currentUserId, long targetUserId, CancellationToken cancellationToken)
    {
        var citizen = await EnsureLeaderboardCitizenExistsAsync(targetUserId, cancellationToken);
        var points = await BalanceAsync(targetUserId, cancellationToken);
        var submittedComplaints = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == targetUserId, cancellationToken);
        var closedComplaints = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == targetUserId && (entity.Status == ComplaintStatus.Closed || entity.Status == ComplaintStatus.ClosedAuto), cancellationToken);
        var helpfulVerifications = await _db.ComplaintVerifications.AsNoTracking().CountAsync(entity => entity.CitizenId == targetUserId && entity.Decision == VerificationDecision.Approved, cancellationToken);
        var supportedIssues = await _db.ComplaintVotes.AsNoTracking().CountAsync(entity => entity.UserId == targetUserId, cancellationToken);
        var followState = await GetFollowStateAsync(targetUserId, currentUserId, cancellationToken);
        var badgeResponses = await BuildBadgeResponsesAsync(targetUserId, cancellationToken);
        var tierRules = await GetTierRulesInternalAsync(cancellationToken);

        return new LeaderboardCitizenProfileResponse
        {
            UserId = citizen.Id,
            CitizenName = MaskName(citizen.FullName),
            Rank = await CalculateRankAsync(targetUserId, cancellationToken),
            Points = points,
            SubmittedComplaints = submittedComplaints,
            ClosedComplaints = closedComplaints,
            HelpfulVerifications = helpfulVerifications,
            SupportedIssues = supportedIssues,
            Tier = ResolveTier(points, tierRules).Tier,
            FollowerCount = followState.FollowerCount,
            IsFollowing = followState.IsFollowing,
            IsCurrentUser = currentUserId == targetUserId,
            MemberSince = citizen.CreatedAt,
            Badges = badgeResponses.Where(item => item.IsUnlocked).Select(item => item.Name).ToList()
        };
    }

    private async Task<IReadOnlyList<BadgeResponse>> BuildBadgeResponsesAsync(long userId, CancellationToken cancellationToken)
    {
        var points = await BalanceAsync(userId, cancellationToken);
        var closed = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == userId && (entity.Status == ComplaintStatus.Closed || entity.Status == ComplaintStatus.ClosedAuto), cancellationToken);
        var submitted = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == userId, cancellationToken);
        var verified = await _db.ComplaintVerifications.AsNoTracking().CountAsync(entity => entity.CitizenId == userId && entity.Decision == VerificationDecision.Approved, cancellationToken);
        var supported = await _db.ComplaintVotes.AsNoTracking().CountAsync(entity => entity.UserId == userId, cancellationToken);
        var firstUnlock = await _db.ReputationLogs.AsNoTracking().Where(entity => entity.UserId == userId && entity.PointsDelta > 0).MinAsync(entity => (DateTimeOffset?)entity.CreatedAt, cancellationToken);
        var metrics = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Points"] = points,
            ["ClosedComplaints"] = closed,
            ["ApprovedVerifications"] = verified,
            ["SubmittedComplaints"] = submitted,
            ["SupportedIssues"] = supported
        };

        var rules = await GetBadgeRulesInternalAsync(cancellationToken);
        return rules
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.DisplayOrder)
            .ThenBy(rule => rule.Name)
            .Select(rule => Badge(rule, metrics.GetValueOrDefault(rule.Metric), firstUnlock))
            .ToList();
    }

    private async Task<User> EnsureLeaderboardCitizenExistsAsync(long targetUserId, CancellationToken cancellationToken)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(entity =>
            entity.Id == targetUserId &&
            entity.Role == UserRole.Citizen &&
            entity.IsActive &&
            !entity.IsDeleted &&
            !entity.IsSystemAccount, cancellationToken)
            ?? throw new NotFoundException("Citizen profile was not found.");
    }

    private async Task<(int FollowerCount, bool IsFollowing)> GetFollowStateAsync(long targetUserId, long currentUserId, CancellationToken cancellationToken)
    {
        var entityId = targetUserId.ToString();
        var events = await _db.AuditLogs.AsNoTracking()
            .Where(entity => entity.EntityName == CitizenProfileEntity &&
                entity.EntityId == entityId &&
                entity.UserId.HasValue &&
                (entity.Action == CitizenFollowedAction || entity.Action == CitizenUnfollowedAction))
            .OrderByDescending(entity => entity.CreatedAt)
            .ThenByDescending(entity => entity.Id)
            .Select(entity => new { FollowerId = entity.UserId!.Value, entity.Action })
            .ToListAsync(cancellationToken);

        var latestByFollower = events.GroupBy(entity => entity.FollowerId).Select(group => group.First()).ToList();
        return (
            latestByFollower.Count(entity => entity.Action == CitizenFollowedAction),
            latestByFollower.Any(entity => entity.FollowerId == currentUserId && entity.Action == CitizenFollowedAction));
    }

    private async Task<IReadOnlyList<BadgeRuleDto>> GetBadgeRulesInternalAsync(CancellationToken cancellationToken)
    {
        var value = await _db.SystemSettings.AsNoTracking()
            .Where(entity => entity.Key == BadgeRulesSettingKey)
            .Select(entity => entity.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return DeserializeRules(value, DefaultBadgeRules)
            .OrderBy(rule => rule.DisplayOrder)
            .ThenBy(rule => rule.Name)
            .ToList();
    }

    private async Task<IReadOnlyList<TierRuleDto>> GetTierRulesInternalAsync(CancellationToken cancellationToken)
    {
        var value = await _db.SystemSettings.AsNoTracking()
            .Where(entity => entity.Key == TierRulesSettingKey)
            .Select(entity => entity.Value)
            .FirstOrDefaultAsync(cancellationToken);
        return DeserializeRules(value, DefaultTierRules)
            .OrderBy(rule => rule.MinimumPoints)
            .ToList();
    }

    private async Task UpsertRulesSettingAsync<T>(string key, IReadOnlyList<T> rules, string description, long adminId, CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(entity => entity.Key == key, cancellationToken);
        if (setting is null)
        {
            setting = new SystemSetting
            {
                Key = key,
                ValueType = "Json",
                Description = description,
                Group = "Rewards",
                IsPublic = false,
                IsSensitive = false
            };
            _db.SystemSettings.Add(setting);
        }

        setting.Value = JsonSerializer.Serialize(rules, JsonOptions);
        setting.UpdatedByUserId = adminId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private AuditLog CreateCitizenFollowAudit(long currentUserId, long targetUserId, string action) => new()
    {
        UserId = currentUserId,
        UserEmail = _currentUser.Email,
        UserRole = _currentUser.Role,
        Action = action,
        EntityName = CitizenProfileEntity,
        EntityId = targetUserId.ToString(),
        NewValuesJson = JsonSerializer.Serialize(new { targetUserId }, JsonOptions),
        Severity = "Information",
        Success = true,
        HttpStatusCode = 200,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private AuditLog CreateAdminAudit(long adminId, string action, string entityName, string? entityId, object? oldValue, object? newValue) => new()
    {
        UserId = adminId,
        UserEmail = _currentUser.Email,
        UserRole = _currentUser.Role,
        Action = action,
        EntityName = entityName,
        EntityId = entityId,
        OldValuesJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
        NewValuesJson = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
        Severity = action.Contains("Adjusted", StringComparison.OrdinalIgnoreCase) ? "Warning" : "Information",
        Success = true,
        HttpStatusCode = 200,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private async Task<int> CalculateRankAsync(long userId, CancellationToken cancellationToken)
    {
        var balance = await BalanceAsync(userId, cancellationToken);
        var higher = await _db.ReputationLogs.AsNoTracking()
            .GroupBy(entity => entity.UserId)
            .Select(group => group.Sum(entity => entity.PointsDelta))
            .CountAsync(points => points > balance, cancellationToken);
        return higher + 1;
    }

    private async Task<int> BalanceAsync(long userId, CancellationToken cancellationToken) =>
        await _db.ReputationLogs.AsNoTracking().Where(entity => entity.UserId == userId).SumAsync(entity => (int?)entity.PointsDelta, cancellationToken) ?? 0;

    private long RequireCitizenId()
    {
        if (_currentUser.UserId is not long userId)
            throw new UnauthorizedAccessException("Authentication is required.");
        if (_currentUser.Role != nameof(UserRole.Citizen))
            throw new UnauthorizedAccessException("Citizen permission is required.");
        return userId;
    }

    private long RequireAdminId()
    {
        if (_currentUser.UserId is not long userId)
            throw new UnauthorizedAccessException("Authentication is required.");
        if (_currentUser.Role != nameof(UserRole.Admin) && _currentUser.Role != nameof(UserRole.SuperAdmin))
            throw new UnauthorizedAccessException("Administrator permission is required.");
        return userId;
    }

    private static DateTimeOffset? LeaderboardPeriodStart(LeaderboardTimeframe timeframe, DateTimeOffset now) => timeframe switch
    {
        LeaderboardTimeframe.Weekly => new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero).AddDays(-(((int)now.DayOfWeek + 6) % 7)),
        LeaderboardTimeframe.Monthly => new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero),
        LeaderboardTimeframe.Yearly => new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
        _ => null
    };

    private static (string Tier, int NextTier) ResolveTier(int points, IReadOnlyList<TierRuleDto> rules)
    {
        var ordered = rules.OrderBy(rule => rule.MinimumPoints).ToList();
        var current = ordered.LastOrDefault(rule => points >= rule.MinimumPoints) ?? ordered.First();
        var next = ordered.FirstOrDefault(rule => rule.MinimumPoints > points);
        return (current.Name, next?.MinimumPoints ?? current.MinimumPoints);
    }

    private static BadgeResponse Badge(BadgeRuleDto rule, int progress, DateTimeOffset? firstUnlock) => new()
    {
        Code = rule.Code,
        Name = rule.Name,
        Description = rule.Description,
        Icon = rule.Icon,
        IsUnlocked = progress >= rule.Target,
        Progress = Math.Min(progress, rule.Target),
        Target = rule.Target,
        UnlockedAt = progress >= rule.Target ? firstUnlock : null
    };

    private static IReadOnlyList<T> DeserializeRules<T>(string? value, IReadOnlyList<T> defaults)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaults;
        try
        {
            var result = JsonSerializer.Deserialize<List<T>>(value, JsonOptions);
            return result is { Count: > 0 } ? result : defaults;
        }
        catch (JsonException)
        {
            return defaults;
        }
    }

    private static string CanonicalMetric(string metric) => metric.Trim().ToLowerInvariant() switch
    {
        "points" => "Points",
        "closedcomplaints" => "ClosedComplaints",
        "approvedverifications" => "ApprovedVerifications",
        "submittedcomplaints" => "SubmittedComplaints",
        "supportedissues" => "SupportedIssues",
        _ => metric.Trim()
    };

    private static RewardType ParseRewardType(string value) =>
        Enum.TryParse<RewardType>(value, true, out var type)
            ? type
            : throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Reward type is invalid."]);

    private static RedemptionStatus ParseRedemptionStatus(string value) =>
        Enum.TryParse<RedemptionStatus>(value, true, out var status)
            ? status
            : throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Redemption status is invalid."]);

    private static string MaskName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "CivicHero Citizen";
        return parts.Length == 1 ? parts[0] : $"{parts[0]} {parts[^1][0]}.";
    }

    private static RedemptionResponse Map(Redemption entity, string rewardName) => new()
    {
        Id = entity.Id,
        RewardName = rewardName,
        PointsSpent = entity.PointsSpent,
        Status = entity.Status.ToString(),
        RedemptionCode = entity.RedemptionCode,
        CreatedAt = entity.CreatedAt,
        FulfilledAt = entity.FulfilledAt
    };

    private static AdminRewardCatalogResponse MapAdminReward(RewardCatalog entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        PointsCost = entity.PointsCost,
        Type = entity.Type.ToString(),
        StockQuantity = entity.StockQuantity,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static AdminRedemptionResponse MapAdminRedemption(Redemption entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        CitizenName = entity.User.FullName,
        CitizenEmail = entity.User.Email,
        RewardCatalogId = entity.RewardCatalogId,
        RewardName = entity.RewardCatalog.Name,
        PointsSpent = entity.PointsSpent,
        Status = entity.Status.ToString(),
        RedemptionCode = entity.RedemptionCode,
        CreatedAt = entity.CreatedAt,
        FulfilledAt = entity.FulfilledAt
    };
}
