using System.Text.Json;
using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/initiatives")]
public sealed class InitiativesController : ControllerBase
{
    private const string EntityName = "CivicInitiative";
    private const string FollowedAction = "InitiativeFollowed";
    private const string UnfollowedAction = "InitiativeUnfollowed";
    private const string FeedbackAction = "InitiativeFeedbackSubmitted";

    private static readonly IReadOnlyDictionary<string, string> InitiativeTitles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["road-renewal"] = "Smart Road Renewal Programme",
            ["swachh-ward"] = "Swachh Ward Community Mission",
            ["led-upgrade"] = "Safe Streets LED Upgrade",
            ["water-resilience"] = "Urban Water Resilience Scheme"
        };

    private readonly CivicDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public InitiativesController(CivicDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("{initiativeId}/engagement")]
    [AllowAnonymous]
    public async Task<IActionResult> Engagement(string initiativeId, CancellationToken cancellationToken)
    {
        var initiative = RequireInitiative(initiativeId);
        return OkEnvelope("Initiative engagement loaded.", await BuildEngagementAsync(initiative.Id, initiative.Title, cancellationToken));
    }

    [HttpPost("{initiativeId}/follow")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> ToggleFollow(string initiativeId, CancellationToken cancellationToken)
    {
        var initiative = RequireInitiative(initiativeId);
        var user = await RequireCitizenAsync(cancellationToken);
        var latest = await _db.AuditLogs.AsNoTracking()
            .Where(log => log.EntityName == EntityName && log.EntityId == initiative.Id && log.UserId == user.Id &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Select(log => log.Action)
            .FirstOrDefaultAsync(cancellationToken);

        var willFollow = latest != FollowedAction;
        var now = DateTimeOffset.UtcNow;
        var action = willFollow ? FollowedAction : UnfollowedAction;

        _db.AuditLogs.Add(CreateAuditLog(
            user,
            action,
            initiative.Id,
            new
            {
                initiativeId = initiative.Id,
                initiativeTitle = initiative.Title,
                isFollowing = willFollow,
                citizenName = user.FullName,
                citizenEmail = user.Email
            },
            now));

        await AddSupervisorNotificationsAsync(
            title: willFollow ? "Citizen followed an initiative" : "Citizen unfollowed an initiative",
            message: $"{user.FullName} {(willFollow ? "followed" : "unfollowed")} {initiative.Title}.",
            now,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        var engagement = await BuildEngagementAsync(initiative.Id, initiative.Title, cancellationToken);
        return OkEnvelope(willFollow ? "You are now following this initiative." : "Initiative removed from your followed list.", engagement);
    }

    [HttpPost("{initiativeId}/feedback")]
    [Authorize(Policy = PermissionConstants.CitizenOnly)]
    public async Task<IActionResult> SubmitFeedback(
        string initiativeId,
        [FromBody] InitiativeFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var initiative = RequireInitiative(initiativeId);
        var user = await RequireCitizenAsync(cancellationToken);
        var feedback = request.Feedback?.Trim() ?? string.Empty;
        var category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim();

        var errors = new List<string>();
        if (request.Rating is < 1 or > 5) errors.Add("Rating must be between 1 and 5.");
        if (feedback.Length < 10) errors.Add("Feedback must contain at least 10 characters.");
        if (feedback.Length > 1500) errors.Add("Feedback cannot exceed 1500 characters.");
        if (category.Length > 80) errors.Add("Feedback category cannot exceed 80 characters.");
        if (errors.Count > 0) throw new ValidationException(errors);

        var now = DateTimeOffset.UtcNow;
        var audit = CreateAuditLog(
            user,
            FeedbackAction,
            initiative.Id,
            new
            {
                initiativeId = initiative.Id,
                initiativeTitle = initiative.Title,
                rating = request.Rating,
                category,
                feedback,
                citizenName = user.FullName,
                citizenEmail = user.Email
            },
            now);

        _db.AuditLogs.Add(audit);

        var preview = feedback.Length > 180 ? feedback[..180] + "…" : feedback;
        await AddSupervisorNotificationsAsync(
            title: $"New feedback: {initiative.Title}",
            message: $"{user.FullName} rated this initiative {request.Rating}/5. {preview}",
            now,
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        var engagement = await BuildEngagementAsync(initiative.Id, initiative.Title, cancellationToken);
        return OkEnvelope("Thank you. Your feedback was sent to the supervisor team.", new
        {
            feedbackId = audit.Id,
            engagement
        });
    }

    [HttpGet("supervisor/activity")]
    [Authorize(Policy = PermissionConstants.SupervisorOrAbove)]
    public async Task<IActionResult> SupervisorActivity(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(log => log.EntityName == EntityName &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction || log.Action == FeedbackAction))
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Take(5000)
            .ToListAsync(cancellationToken);

        var filtered = string.IsNullOrWhiteSpace(type) || type.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? logs
            : logs.Where(log => TypeMatches(log.Action, type)).ToList();

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapActivity)
            .ToList();

        var summaries = InitiativeTitles.Select(item => BuildSummary(item.Key, item.Value, logs)).ToList();

        return OkEnvelope("Initiative follow and feedback activity loaded.", new
        {
            items,
            summaries,
            page,
            pageSize,
            totalCount = filtered.Count
        });
    }

    private async Task<User> RequireCitizenAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");
        return await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId && !user.IsDeleted && user.IsActive && user.Role == UserRole.Citizen, cancellationToken)
            ?? throw new UnauthorizedAccessException("An active Citizen account is required.");
    }

    private async Task AddSupervisorNotificationsAsync(
        string title,
        string message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var supervisorIds = await _db.Users.AsNoTracking()
            .Where(user => !user.IsDeleted && user.IsActive && user.Role == UserRole.Supervisor)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var supervisorId in supervisorIds)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = supervisorId,
                Title = title,
                Message = message,
                Type = NotificationType.General,
                ReferenceType = EntityName,
                ActionUrl = "/supervisor/initiative-engagement",
                CreatedAt = now
            });
        }
    }

    private async Task<object> BuildEngagementAsync(string initiativeId, string initiativeTitle, CancellationToken cancellationToken)
    {
        var logs = await _db.AuditLogs.AsNoTracking()
            .Where(log => log.EntityName == EntityName && log.EntityId == initiativeId &&
                          (log.Action == FollowedAction || log.Action == UnfollowedAction || log.Action == FeedbackAction))
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .ToListAsync(cancellationToken);

        var followerCount = logs
            .Where(log => log.UserId.HasValue && (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .GroupBy(log => log.UserId!.Value)
            .Count(group => group.First().Action == FollowedAction);

        var currentUserFollowing = false;
        if (_currentUser.UserId.HasValue)
        {
            currentUserFollowing = logs
                .Where(log => log.UserId == _currentUser.UserId && (log.Action == FollowedAction || log.Action == UnfollowedAction))
                .Select(log => log.Action)
                .FirstOrDefault() == FollowedAction;
        }

        var ratings = logs
            .Where(log => log.Action == FeedbackAction)
            .Select(log => ReadInt(log.NewValuesJson, "rating"))
            .Where(rating => rating.HasValue)
            .Select(rating => rating!.Value)
            .ToList();

        return new
        {
            initiativeId,
            initiativeTitle,
            followerCount,
            feedbackCount = ratings.Count,
            averageRating = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1),
            isFollowing = currentUserFollowing
        };
    }

    private static InitiativeActivityItem MapActivity(AuditLog log)
    {
        var initiativeId = log.EntityId ?? string.Empty;
        var initiativeTitle = ReadString(log.NewValuesJson, "initiativeTitle") ??
                              (InitiativeTitles.TryGetValue(initiativeId, out var title) ? title : initiativeId);

        return new InitiativeActivityItem
        {
            Id = log.Id,
            InitiativeId = initiativeId,
            InitiativeTitle = initiativeTitle,
            ActivityType = log.Action switch
            {
                FeedbackAction => "Feedback",
                FollowedAction => "Followed",
                UnfollowedAction => "Unfollowed",
                _ => log.Action
            },
            CitizenName = ReadString(log.NewValuesJson, "citizenName") ?? log.UserEmail ?? "Citizen",
            CitizenEmail = ReadString(log.NewValuesJson, "citizenEmail") ?? log.UserEmail,
            Rating = ReadInt(log.NewValuesJson, "rating"),
            Category = ReadString(log.NewValuesJson, "category"),
            Feedback = ReadString(log.NewValuesJson, "feedback"),
            CreatedAt = log.CreatedAt
        };
    }

    private static InitiativeSummaryItem BuildSummary(string initiativeId, string initiativeTitle, IReadOnlyCollection<AuditLog> logs)
    {
        var initiativeLogs = logs.Where(log => string.Equals(log.EntityId, initiativeId, StringComparison.OrdinalIgnoreCase)).ToList();
        var followerCount = initiativeLogs
            .Where(log => log.UserId.HasValue && (log.Action == FollowedAction || log.Action == UnfollowedAction))
            .GroupBy(log => log.UserId!.Value)
            .Count(group => group.First().Action == FollowedAction);
        var ratings = initiativeLogs
            .Where(log => log.Action == FeedbackAction)
            .Select(log => ReadInt(log.NewValuesJson, "rating"))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        return new InitiativeSummaryItem
        {
            InitiativeId = initiativeId,
            InitiativeTitle = initiativeTitle,
            FollowerCount = followerCount,
            FeedbackCount = ratings.Count,
            AverageRating = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1)
        };
    }

    private AuditLog CreateAuditLog(User user, string action, string initiativeId, object values, DateTimeOffset createdAt) => new()
    {
        UserId = user.Id,
        UserEmail = user.Email,
        UserRole = user.Role.ToString(),
        Action = action,
        EntityName = EntityName,
        EntityId = initiativeId,
        NewValuesJson = JsonSerializer.Serialize(values),
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = Request.Headers.UserAgent.ToString(),
        CorrelationId = HttpContext.TraceIdentifier,
        Severity = "Information",
        Success = true,
        HttpStatusCode = StatusCodes.Status200OK,
        CreatedAt = createdAt
    };

    private static bool TypeMatches(string action, string type) => type.Trim().ToLowerInvariant() switch
    {
        "feedback" => action == FeedbackAction,
        "followed" => action == FollowedAction,
        "unfollowed" => action == UnfollowedAction,
        "follow" => action == FollowedAction || action == UnfollowedAction,
        _ => true
    };

    private static (string Id, string Title) RequireInitiative(string initiativeId)
    {
        var id = initiativeId?.Trim() ?? string.Empty;
        if (!InitiativeTitles.TryGetValue(id, out var title))
            throw new NotFoundException("Initiative was not found.");
        return (id, title);
    }

    private static string? ReadString(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int? ReadInt(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
                ? result
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private IActionResult OkEnvelope<T>(string message, T data) => Ok(new { success = true, message, data });
}

public sealed class InitiativeFeedbackRequest
{
    public int Rating { get; set; }
    public string Category { get; set; } = "General";
    public string Feedback { get; set; } = string.Empty;
}

public sealed class InitiativeActivityItem
{
    public long Id { get; set; }
    public string InitiativeId { get; set; } = string.Empty;
    public string InitiativeTitle { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string CitizenName { get; set; } = string.Empty;
    public string? CitizenEmail { get; set; }
    public int? Rating { get; set; }
    public string? Category { get; set; }
    public string? Feedback { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class InitiativeSummaryItem
{
    public string InitiativeId { get; set; } = string.Empty;
    public string InitiativeTitle { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int FeedbackCount { get; set; }
    public double AverageRating { get; set; }
}
