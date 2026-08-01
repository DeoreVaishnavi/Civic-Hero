using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CivicHero.Backend.Core.DTOs.Chatbot;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed partial class ChatbotService : IChatbotService
{
    private const int MaximumMessageLength = 1000;
    private readonly IChatRepository _chatRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ChatbotService(
        IChatRepository chatRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _chatRepository = chatRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ChatSessionResponse> StartSessionAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var now = DateTimeOffset.UtcNow;
        var session = new ChatSession
        {
            UserId = userId,
            SessionId = Guid.NewGuid().ToString("N"),
            Status = "Active",
            StartedAt = now
        };

        await _chatRepository.AddSessionAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var greeting = new ChatMessage
        {
            ChatSessionId = session.Id,
            Sender = MessageSender.Assistant,
            SentAt = now,
            Content = "Hello! I am the CivicHero assistant. I can help you report an issue, check a complaint status, understand verification or disputes, and find rewards. Try: ‘Check CH-2026-000123’."
        };
        await _chatRepository.AddMessageAsync(greeting, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        session.Messages.Add(greeting);

        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    public async Task<ChatSessionResponse> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, false, cancellationToken);
        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSessionHistoryItemResponse>> ListSessionsAsync(
        string? search,
        int take,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var limit = Math.Clamp(take, 1, 50);
        var sessions = await _chatRepository.ListOwnedSessionsAsync(userId, 100, cancellationToken);
        var titles = await LoadSessionTitlesAsync(userId, sessions.Select(session => session.SessionId), cancellationToken);
        var normalizedSearch = search?.Trim();

        var results = sessions
            .Where(session => string.IsNullOrWhiteSpace(normalizedSearch) ||
                (titles.GetValueOrDefault(session.SessionId) ?? DefaultTitle(session))
                    .Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                session.SessionId.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                session.Messages.Any(message =>
                    message.Content.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)))
            .Select(session => MapHistoryItem(session, titles.GetValueOrDefault(session.SessionId) ?? DefaultTitle(session)))
            .OrderByDescending(item => item.LastMessageAt)
            .Take(limit)
            .ToList();

        return results;
    }

    public async Task<ChatSessionResponse> RenameSessionAsync(
        string sessionId,
        RenameChatSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is < 3 or > 80)
            throw new ValidationException(["Chat title must contain between 3 and 80 characters."]);

        var session = await FindOwnedSessionAsync(sessionId, false, cancellationToken);
        await AddAuditAsync(
            "ChatSessionRenamed",
            "ChatSession",
            session.SessionId,
            new { title },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildSessionResponseAsync(session, cancellationToken, title);
    }

    public async Task<ChatSessionResponse> ContinueSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, true, cancellationToken);
        session.Status = "Active";
        session.EndedAt = null;

        await AddAuditAsync(
            "ChatSessionContinued",
            "ChatSession",
            session.SessionId,
            new { status = session.Status },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildSessionResponseAsync(session, cancellationToken);
    }

    public async Task<ChatReplyResponse> SendMessageAsync(ChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var messageText = NormalizeMessage(request.Message);
        var session = await FindOwnedSessionAsync(request.SessionId, true, cancellationToken);
        if (!string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleViolationException("This chat session has ended. Start a new session to continue.");

        var now = DateTimeOffset.UtcNow;
        var userMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Sender = MessageSender.Citizen,
            Content = messageText,
            SentAt = now
        };
        await _chatRepository.AddMessageAsync(userMessage, cancellationToken);

        var result = await GenerateReplyAsync(messageText, cancellationToken);
        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Sender = MessageSender.Assistant,
            Content = result.Content,
            SentAt = DateTimeOffset.UtcNow
        };
        await _chatRepository.AddMessageAsync(assistantMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatReplyResponse
        {
            SessionId = session.SessionId,
            UserMessage = MapMessage(userMessage),
            AssistantMessage = MapMessage(assistantMessage, result.ActionLabel, result.ActionUrl),
            Intent = result.Intent,
            Confidence = result.Confidence
        };
    }

    public async Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, true, cancellationToken);
        if (!string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase))
        {
            session.Status = "Ended";
            session.EndedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ChatMessageFeedbackResponse> SubmitFeedbackAsync(
        string sessionId,
        long messageId,
        ChatMessageFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, false, cancellationToken);
        var message = session.Messages.FirstOrDefault(candidate => candidate.Id == messageId)
            ?? throw new NotFoundException("Chat message was not found.");

        if (message.Sender != MessageSender.Assistant)
            throw new BusinessRuleViolationException("Feedback can only be submitted for an assistant response.");

        var comment = request.Comment?.Trim();
        if (comment?.Length > 500)
            throw new ValidationException(["Feedback comment cannot exceed 500 characters."]);

        var now = DateTimeOffset.UtcNow;
        await AddAuditAsync(
            "ChatMessageFeedback",
            "ChatMessage",
            message.Id.ToString(),
            new
            {
                sessionId = session.SessionId,
                helpful = request.Helpful,
                comment,
                submittedAt = now
            },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatMessageFeedbackResponse
        {
            MessageId = message.Id,
            Feedback = request.Helpful ? "Helpful" : "NotHelpful",
            Comment = comment,
            SubmittedAt = now
        };
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await FindOwnedSessionAsync(sessionId, true, cancellationToken);
        await AddAuditAsync(
            "ChatSessionDeleted",
            "ChatSession",
            session.SessionId,
            new
            {
                session.Status,
                session.StartedAt,
                session.EndedAt,
                messageCount = session.Messages.Count
            },
            cancellationToken);

        _chatRepository.RemoveSession(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<BotResult> GenerateReplyAsync(string text, CancellationToken cancellationToken)
    {
        var normalized = text.ToLowerInvariant();
        var complaintId = ExtractComplaintId(text);

        if (complaintId.HasValue || ContainsAny(normalized, "status", "track complaint", "complaint update"))
        {
            if (!complaintId.HasValue)
            {
                return new BotResult(
                    "ComplaintStatus",
                    0.96m,
                    "Please include your complaint reference, for example CH-2026-000123, so I can look up its current status.",
                    "Open my complaints",
                    "/citizen/complaints");
            }

            return await BuildComplaintStatusReplyAsync(complaintId.Value, cancellationToken);
        }

        if (ContainsAny(normalized, "my complaint", "open complaint", "recent complaint"))
            return await BuildComplaintSummaryReplyAsync(cancellationToken);

        if (ContainsAny(normalized, "report", "submit complaint", "new complaint", "raise issue", "pothole", "garbage", "streetlight", "water leak"))
        {
            return new BotResult(
                "ReportComplaint",
                0.94m,
                "To report an issue: open Report Issue, select the category and department, describe the problem, capture GPS, attach clear photos, and submit. Keep location permission enabled for accurate routing.",
                "Report an issue",
                "/citizen/report");
        }

        if (ContainsAny(normalized, "verify", "verification", "approve resolution", "reject resolution"))
        {
            return new BotResult(
                "VerificationHelp",
                0.93m,
                "When an officer submits resolution evidence, open Verify Resolutions. Review the before/after evidence near the complaint location, provide a rating, then approve or reject. A rejection opens a dispute for supervisor review.",
                "Open verifications",
                "/citizen/verifications");
        }

        if (ContainsAny(normalized, "dispute", "appeal", "not fixed", "reopen"))
        {
            return new BotResult(
                "DisputeHelp",
                0.92m,
                "Reject a resolution when the issue is not actually fixed and clearly explain why. CivicHero creates a dispute for supervisor review. After the supervisor decision, an eligible case can be appealed from Disputes & Appeals.",
                "View disputes",
                "/citizen/disputes");
        }

        if (ContainsAny(normalized, "reward", "points", "badge", "leaderboard", "redeem"))
        {
            return new BotResult(
                "RewardsHelp",
                0.91m,
                "You earn civic points for valid participation, such as a successfully closed complaint and citizen-approved verification. Open Rewards & Rank to view points, badges, transaction history and available redemptions.",
                "Open rewards",
                "/citizen/rewards");
        }

        if (ContainsAny(normalized, "notification", "alert", "message"))
        {
            return new BotResult(
                "NotificationHelp",
                0.89m,
                "CivicHero sends live alerts for complaint progress, assignments, verification, disputes and rewards. Open Notifications to read alerts or change delivery preferences.",
                "Open notifications",
                RolePath("notifications"));
        }

        if (ContainsAny(normalized, "hello", "hi", "hey", "good morning", "good evening", "help"))
        {
            return new BotResult(
                "Greeting",
                0.88m,
                "I can help with: reporting an issue, checking a complaint reference, verification, disputes, rewards and notifications. Ask a question or choose one of the quick actions.",
                null,
                null);
        }

        return new BotResult(
            "GeneralHelp",
            0.62m,
            "I could not confidently match that request. Try asking: ‘How do I report a pothole?’, ‘Check CH-2026-000123’, ‘How does verification work?’, or ‘Show my rewards’. For an urgent safety emergency, contact the appropriate local emergency service instead of relying on the chatbot.",
            "Open dashboard",
            RolePath(string.Empty));
    }

    private async Task<BotResult> BuildComplaintStatusReplyAsync(long complaintId, CancellationToken cancellationToken)
    {
        var complaint = await VisibleComplaints()
            .Include(entity => entity.Department)
            .Include(entity => entity.Ward)
            .FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken);

        if (complaint is null)
        {
            return new BotResult(
                "ComplaintStatus",
                0.99m,
                "I could not find that complaint in your permitted CivicHero records. Check the reference number or open your complaint list.",
                "Open complaints",
                RolePath("complaints"));
        }

        var reference = $"CH-{complaint.CreatedAt:yyyy}-{complaint.Id:D6}";
        var builder = new StringBuilder();
        builder.Append($"{reference} — {complaint.Title}. ");
        builder.Append($"Current status: {Humanize(complaint.Status.ToString())}. ");
        builder.Append($"Priority: {complaint.Priority}. Department: {complaint.Department.Name}; ward: {complaint.Ward.Name}. ");

        if (complaint.ResolvedAt.HasValue)
            builder.Append($"Resolution was submitted on {complaint.ResolvedAt.Value:dd MMM yyyy, h:mm tt}. ");
        else
            builder.Append($"Last updated on {complaint.UpdatedAt:dd MMM yyyy, h:mm tt}. ");

        builder.Append(StatusGuidance(complaint.Status));

        return new BotResult(
            "ComplaintStatus",
            0.99m,
            builder.ToString(),
            "Open complaint",
            ComplaintPath(complaint.Id));
    }

    private async Task<BotResult> BuildComplaintSummaryReplyAsync(CancellationToken cancellationToken)
    {
        var complaints = await VisibleComplaints()
            .OrderByDescending(entity => entity.CreatedAt)
            .Take(5)
            .Select(entity => new { entity.Id, entity.Title, entity.Status, entity.CreatedAt })
            .ToListAsync(cancellationToken);

        if (complaints.Count == 0)
        {
            return new BotResult(
                "ComplaintSummary",
                0.95m,
                "No complaints are currently available in your permitted records.",
                "Report an issue",
                "/citizen/report");
        }

        var summary = string.Join("; ", complaints.Select(entity =>
            $"CH-{entity.CreatedAt:yyyy}-{entity.Id:D6}: {entity.Title} ({Humanize(entity.Status.ToString())})"));

        return new BotResult(
            "ComplaintSummary",
            0.95m,
            $"Here are the most recent complaints I can show you: {summary}.",
            "Open complaints",
            RolePath("complaints"));
    }

    private IQueryable<Complaint> VisibleComplaints()
    {
        var userId = RequireUserId();
        var query = _unitOfWork.Repository<Complaint>().Query();
        var role = _currentUser.Role;

        if (string.Equals(role, nameof(UserRole.Citizen), StringComparison.OrdinalIgnoreCase))
            return query.Where(entity => entity.CitizenId == userId && !entity.IsDeleted);

        if (string.Equals(role, nameof(UserRole.Officer), StringComparison.OrdinalIgnoreCase))
            return query.Where(entity => entity.AssignedOfficerId == userId && !entity.IsDeleted);

        if (string.Equals(role, nameof(UserRole.Supervisor), StringComparison.OrdinalIgnoreCase) && _currentUser.DepartmentId.HasValue)
            return query.Where(entity => entity.DepartmentId == _currentUser.DepartmentId.Value && !entity.IsDeleted);

        if (string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
            return query.Where(entity => !entity.IsDeleted);

        return query.Where(_ => false);
    }

    private async Task<ChatSession> FindOwnedSessionAsync(string sessionId, bool tracking, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ValidationException(["SessionId is required."]);

        return await _chatRepository.FindOwnedSessionAsync(sessionId.Trim(), RequireUserId(), tracking, cancellationToken)
               ?? throw new NotFoundException("Chat session was not found.");
    }

    private static string NormalizeMessage(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ValidationException(["Message is required."]);
        if (normalized.Length > MaximumMessageLength)
            throw new ValidationException([$"Message cannot exceed {MaximumMessageLength} characters."]);
        return normalized;
    }

    private long RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("Authentication is required.");

    private static long? ExtractComplaintId(string text)
    {
        var referenceMatch = ComplaintReferenceRegex().Match(text);
        if (referenceMatch.Success && long.TryParse(referenceMatch.Groups["id"].Value, out var referenceId))
            return referenceId;

        var numericMatch = ComplaintNumberRegex().Match(text);
        return numericMatch.Success && long.TryParse(numericMatch.Groups["id"].Value, out var numericId)
            ? numericId
            : null;
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private string ComplaintPath(long complaintId) => _currentUser.Role switch
    {
        nameof(UserRole.Officer) => $"/officer/assignments/{complaintId}",
        nameof(UserRole.Supervisor) => $"/supervisor/assignments/{complaintId}",
        nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin) => "/admin/analytics",
        _ => $"/citizen/complaints/{complaintId}"
    };

    private string RolePath(string suffix)
    {
        var root = _currentUser.Role switch
        {
            nameof(UserRole.Officer) => "/officer",
            nameof(UserRole.Supervisor) => "/supervisor",
            nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin) => "/admin",
            _ => "/citizen"
        };

        if (string.IsNullOrWhiteSpace(suffix)) return root;
        if (suffix == "complaints" && root == "/officer") return "/officer/assignments";
        if (suffix == "complaints" && root == "/supervisor") return "/supervisor/assignments";
        if (suffix == "complaints" && root == "/admin") return "/admin/analytics";
        return $"{root}/{suffix}";
    }

    private static string StatusGuidance(ComplaintStatus status) => status switch
    {
        ComplaintStatus.Created or ComplaintStatus.AiTriage => "The issue is being validated and routed.",
        ComplaintStatus.FraudReview => "The submission is awaiting administrative review.",
        ComplaintStatus.Assigned => "An officer has been assigned.",
        ComplaintStatus.ReassignmentPending => "The supervisor is selecting another officer.",
        ComplaintStatus.InProgress => "Field work is currently in progress.",
        ComplaintStatus.Escalated => "The case has crossed an SLA threshold and is escalated.",
        ComplaintStatus.Resolved or ComplaintStatus.VerificationPending => "Please review the resolution evidence when verification becomes available.",
        ComplaintStatus.Disputed or ComplaintStatus.Appealed => "The case is under formal review.",
        ComplaintStatus.Closed or ComplaintStatus.ClosedAuto => "The complaint lifecycle is complete.",
        ComplaintStatus.ClosedFraud => "The complaint was closed after fraud review.",
        ComplaintStatus.Merged => "This complaint was merged with a related civic issue.",
        ComplaintStatus.Withdrawn => "The citizen withdrew this complaint.",
        _ => "Open the complaint page for the complete timeline."
    };

    private static string Humanize(string value) =>
        Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");

    private async Task<ChatSessionResponse> BuildSessionResponseAsync(
        ChatSession session,
        CancellationToken cancellationToken,
        string? titleOverride = null)
    {
        var userId = RequireUserId();
        var title = titleOverride ??
            (await LoadSessionTitlesAsync(userId, [session.SessionId], cancellationToken))
                .GetValueOrDefault(session.SessionId) ??
            DefaultTitle(session);
        var feedback = await LoadMessageFeedbackAsync(userId, session.Messages.Select(message => message.Id), cancellationToken);

        return new ChatSessionResponse
        {
            SessionId = session.SessionId,
            Title = title,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Messages = session.Messages
                .OrderBy(message => message.SentAt)
                .Select(message =>
                {
                    feedback.TryGetValue(message.Id, out var entry);
                    return MapMessage(message, feedback: entry);
                })
                .ToList()
        };
    }

    private async Task<Dictionary<string, string>> LoadSessionTitlesAsync(
        long userId,
        IEnumerable<string> sessionIds,
        CancellationToken cancellationToken)
    {
        var ids = sessionIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var logs = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.UserId == userId &&
                log.Action == "ChatSessionRenamed" &&
                log.EntityName == "ChatSession" &&
                log.EntityId != null &&
                ids.Contains(log.EntityId))
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);

        var titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logs)
        {
            if (log.EntityId is null || titles.ContainsKey(log.EntityId)) continue;
            var payload = Deserialize<SessionTitleAuditPayload>(log.NewValuesJson);
            if (!string.IsNullOrWhiteSpace(payload?.Title)) titles[log.EntityId] = payload.Title.Trim();
        }

        return titles;
    }

    private async Task<Dictionary<long, MessageFeedbackMetadata>> LoadMessageFeedbackAsync(
        long userId,
        IEnumerable<long> messageIds,
        CancellationToken cancellationToken)
    {
        var ids = messageIds.Distinct().Select(id => id.ToString()).ToList();
        if (ids.Count == 0) return [];

        var logs = await _unitOfWork.Repository<AuditLog>().Query()
            .Where(log => log.UserId == userId &&
                log.Action == "ChatMessageFeedback" &&
                log.EntityName == "ChatMessage" &&
                log.EntityId != null &&
                ids.Contains(log.EntityId))
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);

        var feedback = new Dictionary<long, MessageFeedbackMetadata>();
        foreach (var log in logs)
        {
            if (!long.TryParse(log.EntityId, out var messageId) || feedback.ContainsKey(messageId)) continue;
            var payload = Deserialize<MessageFeedbackAuditPayload>(log.NewValuesJson);
            if (payload is null) continue;
            feedback[messageId] = new MessageFeedbackMetadata(payload.Helpful ? "Helpful" : "NotHelpful", payload.Comment);
        }

        return feedback;
    }

    private async Task AddAuditAsync(
        string action,
        string entityName,
        string entityId,
        object values,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserId = RequireUserId(),
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            NewValuesJson = JsonSerializer.Serialize(values),
            Severity = "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private static ChatSessionHistoryItemResponse MapHistoryItem(ChatSession session, string title)
    {
        var lastMessage = session.Messages.OrderByDescending(message => message.SentAt).FirstOrDefault();
        var preview = lastMessage?.Content.Trim() ?? "No messages yet.";
        if (preview.Length > 120) preview = $"{preview[..117]}...";

        return new ChatSessionHistoryItemResponse
        {
            SessionId = session.SessionId,
            Title = title,
            Status = session.Status,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            LastMessageAt = lastMessage?.SentAt ?? session.StartedAt,
            MessageCount = session.Messages.Count,
            Preview = preview
        };
    }

    private static string DefaultTitle(ChatSession session)
    {
        var firstCitizenMessage = session.Messages
            .Where(message => message.Sender == MessageSender.Citizen)
            .OrderBy(message => message.SentAt)
            .Select(message => message.Content.Trim())
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstCitizenMessage)) return "New CivicHero conversation";
        return firstCitizenMessage.Length <= 60 ? firstCitizenMessage : $"{firstCitizenMessage[..57]}...";
    }

    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static ChatMessageResponse MapMessage(
        ChatMessage message,
        string? actionLabel = null,
        string? actionUrl = null,
        MessageFeedbackMetadata? feedback = null) => new()
    {
        Id = message.Id,
        Sender = message.Sender.ToString(),
        Content = message.Content,
        SentAt = message.SentAt,
        ActionLabel = actionLabel,
        ActionUrl = actionUrl,
        Feedback = feedback?.Feedback,
        FeedbackComment = feedback?.Comment
    };

    [GeneratedRegex(@"CH-(?:\d{4})-(?<id>\d{1,12})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ComplaintReferenceRegex();

    [GeneratedRegex(@"(?:complaint|issue)\s*(?:id|number|no\.?|#)?\s*[:#-]?\s*(?<id>\d{1,12})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ComplaintNumberRegex();

    private sealed record SessionTitleAuditPayload(string Title);
    private sealed record MessageFeedbackAuditPayload(bool Helpful, string? Comment);
    private sealed record MessageFeedbackMetadata(string Feedback, string? Comment);

    private sealed record BotResult(
        string Intent,
        decimal Confidence,
        string Content,
        string? ActionLabel,
        string? ActionUrl);
}
