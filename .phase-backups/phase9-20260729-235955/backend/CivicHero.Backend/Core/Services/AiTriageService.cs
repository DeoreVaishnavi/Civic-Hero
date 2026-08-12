using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class AiTriageService : IAiTriageService
{
    private readonly CivicDbContext _db;
    private readonly GeminiAiService _gemini;
    private readonly RuleBasedAiService _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly AiOptions _options;

    public AiTriageService(CivicDbContext db, GeminiAiService gemini, RuleBasedAiService rules, ICurrentUserService currentUser, IOptions<AiOptions> options)
    {
        _db = db;
        _gemini = gemini;
        _rules = rules;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<ClassificationResponse> ClassifyAsync(AiTextRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ProviderAnalysisAsync(request, cancellationToken);
        var department = await FindDepartmentAsync(result.Category, cancellationToken);
        return new ClassificationResponse(result.Category, department?.Id, department?.Name, result.ClassificationConfidence, result.Provider, result.Reasoning);
    }

    public async Task<DuplicateCheckResponse> CheckDuplicateAsync(AiTextRequest request, long? excludeComplaintId = null, CancellationToken cancellationToken = default)
    {
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return new DuplicateCheckResponse("LocationRequired", 0m, null, [], "Latitude and longitude are required for duplicate detection.");

        var radiusKm = Math.Clamp(_options.DuplicateRadiusMeters, 100, 2000) / 1000d;
        var latitudeDelta = radiusKm / 111d;
        var longitudeFactor = Math.Max(0.2d, Math.Cos((double)request.Latitude.Value * Math.PI / 180d));
        var longitudeDelta = radiusKm / (111d * longitudeFactor);
        var minLat = request.Latitude.Value - (decimal)latitudeDelta;
        var maxLat = request.Latitude.Value + (decimal)latitudeDelta;
        var minLon = request.Longitude.Value - (decimal)longitudeDelta;
        var maxLon = request.Longitude.Value + (decimal)longitudeDelta;

        var candidates = await _db.Complaints.AsNoTracking()
            .Where(entity => (!excludeComplaintId.HasValue || entity.Id != excludeComplaintId.Value)
                && entity.Status != ComplaintStatus.Withdrawn
                && entity.Status != ComplaintStatus.ClosedFraud
                && entity.Latitude >= minLat && entity.Latitude <= maxLat
                && entity.Longitude >= minLon && entity.Longitude <= maxLon)
            .OrderByDescending(entity => entity.CreatedAt)
            .Take(100)
            .Select(entity => new { entity.Id, entity.Title, entity.Description, entity.Latitude, entity.Longitude })
            .ToListAsync(cancellationToken);

        var inputText = $"{request.Title} {request.Description}";
        var matches = candidates.Select(entity =>
        {
            var distance = HaversineMeters(request.Latitude.Value, request.Longitude.Value, entity.Latitude, entity.Longitude);
            var textScore = TextSimilarity(inputText, $"{entity.Title} {entity.Description}");
            var distanceScore = Math.Max(0m, 1m - ((decimal)distance / Math.Max(1, _options.DuplicateRadiusMeters)));
            var combined = Math.Clamp((textScore * 0.72m) + (distanceScore * 0.28m), 0m, 1m);
            return new DuplicateMatchResponse(entity.Id, entity.Title, Math.Round((decimal)distance, 1), Math.Round(textScore, 4), Math.Round(combined, 4));
        }).Where(item => item.DistanceMeters <= _options.DuplicateRadiusMeters)
          .OrderByDescending(item => item.CombinedScore).Take(5).ToList();

        var best = matches.FirstOrDefault();
        var score = best?.CombinedScore ?? 0m;
        var status = score >= _options.ConfirmedDuplicateThreshold ? "ConfirmedDuplicate"
            : score >= _options.PossibleDuplicateThreshold ? "PossibleDuplicate" : "Unique";
        var reasoning = best is null ? "No nearby complaint candidates were found."
            : $"Best match is complaint #{best.ComplaintId} at {best.DistanceMeters:0} metres with {best.TextSimilarity:P0} text similarity.";
        return new DuplicateCheckResponse(status, score, best?.ComplaintId, matches, reasoning);
    }

    public async Task<FraudCheckResponse> CheckFraudAsync(AiTextRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ProviderAnalysisAsync(request, cancellationToken);
        return await BuildFraudResponseAsync(request, result, cancellationToken);
    }

    public async Task<PriorityPredictionResponse> PredictPriorityAsync(AiTextRequest request, CancellationToken cancellationToken = default)
    {
        var result = await ProviderAnalysisAsync(request, cancellationToken);
        return new PriorityPredictionResponse(result.Priority.ToString(), result.PriorityScore, [result.Reasoning], result.Provider);
    }

    public async Task<AiTriageResponse> AnalyzeComplaintAsync(long complaintId, bool force = false, CancellationToken cancellationToken = default)
    {
        var complaint = await _db.Complaints.Include(entity => entity.Citizen).FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        if (!force && complaint.AiTriagedAt.HasValue)
        {
            var latest = await _db.AiTriageAnalyses.AsNoTracking().Where(entity => entity.ComplaintId == complaintId)
                .OrderByDescending(entity => entity.AnalyzedAt).FirstOrDefaultAsync(cancellationToken);
            if (latest is not null) return Map(complaint, latest);
        }

        complaint.Status = ComplaintStatus.AiTriage;

        var input = new AiTextRequest(complaint.Title, complaint.Description, complaint.Category, complaint.Latitude, complaint.Longitude, complaint.CitizenId);
        var provider = await ProviderAnalysisAsync(input, cancellationToken);
        var department = await FindDepartmentAsync(provider.Category, cancellationToken);
        var classification = new ClassificationResponse(provider.Category, department?.Id, department?.Name, provider.ClassificationConfidence, provider.Provider, provider.Reasoning);
        var duplicate = await CheckDuplicateAsync(input, complaint.Id, cancellationToken);
        var fraud = await BuildFraudResponseAsync(input, provider, cancellationToken);
        var priority = new PriorityPredictionResponse(provider.Priority.ToString(), provider.PriorityScore, [provider.Reasoning], provider.Provider);
        var requiresReview = duplicate.Status == "PossibleDuplicate" || fraud.RequiresManualReview || classification.Confidence < 0.60m;

        var analysis = new AiTriageAnalysis
        {
            ComplaintId = complaint.Id,
            Provider = provider.Provider,
            Model = provider.Model,
            PredictedCategory = classification.Category,
            PredictedDepartmentId = classification.DepartmentId,
            ClassificationConfidence = classification.Confidence,
            DuplicateStatus = duplicate.Status,
            DuplicateComplaintId = duplicate.MatchingComplaintId,
            DuplicateScore = duplicate.Score,
            FraudVerdict = fraud.Verdict,
            FraudScore = fraud.Score,
            PredictedPriority = provider.Priority,
            PriorityScore = provider.PriorityScore,
            RequiresManualReview = requiresReview,
            Reasoning = $"{classification.Reasoning} {duplicate.Reasoning} {string.Join(" ", fraud.Signals)}".Trim(),
            RawProviderResponse = provider.RawResponse,
            AnalyzedAt = DateTimeOffset.UtcNow
        };
        _db.AiTriageAnalyses.Add(analysis);
        _db.AiFraudAnalyses.Add(new AiFraudAnalysis
        {
            ComplaintId = complaint.Id,
            FraudScore = (float)fraud.Score,
            Verdict = fraud.Verdict,
            Reasoning = string.Join(" ", fraud.Signals),
            Provider = provider.Provider,
            Model = provider.Model,
            RequiresManualReview = fraud.RequiresManualReview,
            AnalyzedAt = DateTimeOffset.UtcNow
        });

        complaint.AiTriagedAt = analysis.AnalyzedAt;
        complaint.AiSuggestedCategory = classification.Category;
        complaint.AiConfidence = classification.Confidence;
        complaint.AiRiskScore = fraud.Score;
        complaint.Priority = provider.Priority;
        complaint.DuplicateOfComplaintId = duplicate.Status == "ConfirmedDuplicate" ? duplicate.MatchingComplaintId : null;
        complaint.Status = duplicate.Status == "ConfirmedDuplicate" ? ComplaintStatus.Merged
            : fraud.Score >= _options.FraudReviewThreshold ? ComplaintStatus.FraudReview
            : ComplaintStatus.Created;
        complaint.Timeline.Add(new ComplaintTimeline
        {
            EventType = "AI_TRIAGE_COMPLETED",
            Description = $"AI suggested {classification.Category}, {provider.Priority} priority, duplicate status {duplicate.Status}, fraud verdict {fraud.Verdict}.",
            Timestamp = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Map(complaint, analysis);
    }

    public async Task<IReadOnlyList<AiReviewQueueItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.AiTriageAnalyses.AsNoTracking().Include(entity => entity.Complaint).ThenInclude(entity => entity.Citizen)
            .Where(entity => entity.RequiresManualReview && entity.ReviewedAt == null)
            .OrderByDescending(entity => entity.AnalyzedAt).Take(250).ToListAsync(cancellationToken);
        return rows.GroupBy(entity => entity.ComplaintId).Select(group => group.First()).Select(entity => new AiReviewQueueItem(
            entity.ComplaintId, entity.Complaint.Title, entity.Complaint.Citizen.FullName, entity.Complaint.Status.ToString(),
            entity.PredictedCategory, entity.PredictedPriority.ToString(), entity.DuplicateStatus, entity.DuplicateScore,
            entity.FraudVerdict, entity.FraudScore, entity.Reasoning, entity.AnalyzedAt)).ToList();
    }

    public async Task<AiTriageResponse> DecideAsync(long complaintId, AiReviewDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var complaint = await _db.Complaints.FirstOrDefaultAsync(entity => entity.Id == complaintId, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");
        var analysis = await _db.AiTriageAnalyses.Where(entity => entity.ComplaintId == complaintId && entity.ReviewedAt == null)
            .OrderByDescending(entity => entity.AnalyzedAt).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No pending AI review was found for this complaint.");
        var decision = request.Decision.Trim();
        analysis.ReviewDecision = decision;
        analysis.ReviewNotes = request.Notes?.Trim();
        analysis.ReviewedByUserId = _currentUser.UserId;
        analysis.ReviewedAt = DateTimeOffset.UtcNow;
        analysis.RequiresManualReview = false;

        if (decision.Equals("Clear", StringComparison.OrdinalIgnoreCase))
        {
            complaint.Status = ComplaintStatus.Created;
            complaint.DuplicateOfComplaintId = null;
        }
        else if (decision.Equals("ConfirmFraud", StringComparison.OrdinalIgnoreCase))
        {
            complaint.Status = ComplaintStatus.ClosedFraud;
            complaint.ClosedAt = DateTimeOffset.UtcNow;
        }
        else if (decision.Equals("Merge", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.MergeIntoComplaintId.HasValue || request.MergeIntoComplaintId.Value == complaintId)
                throw new ValidationException(["A different parent complaint ID is required for merge."]);
            var parentExists = await _db.Complaints.AsNoTracking().AnyAsync(entity => entity.Id == request.MergeIntoComplaintId.Value, cancellationToken);
            if (!parentExists) throw new NotFoundException("The parent complaint was not found.");
            complaint.Status = ComplaintStatus.Merged;
            complaint.DuplicateOfComplaintId = request.MergeIntoComplaintId.Value;
        }
        else if (!decision.Equals("Reanalyze", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(["Decision must be Clear, ConfirmFraud, Merge, or Reanalyze."]);
        }

        complaint.Timeline.Add(new ComplaintTimeline
        {
            UserId = _currentUser.UserId,
            EventType = "AI_REVIEW_DECISION",
            Description = $"Manual AI review decision: {decision}. {request.Notes}".Trim(),
            Timestamp = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return decision.Equals("Reanalyze", StringComparison.OrdinalIgnoreCase)
            ? await AnalyzeComplaintAsync(complaintId, true, cancellationToken)
            : Map(complaint, analysis);
    }

    public async Task<IReadOnlyList<HotspotResponse>> GetHotspotsAsync(int days, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 7, 365);
        var since = DateTimeOffset.UtcNow.AddDays(-days);
        var rows = await _db.Complaints.AsNoTracking().Where(entity => entity.CreatedAt >= since && entity.Status != ComplaintStatus.Withdrawn && entity.Status != ComplaintStatus.ClosedFraud)
            .Select(entity => new { entity.Latitude, entity.Longitude, entity.Category, entity.Priority }).ToListAsync(cancellationToken);
        return rows.GroupBy(entity => new { Lat = Math.Round(entity.Latitude, 3), Lon = Math.Round(entity.Longitude, 3) })
            .Select(group =>
            {
                var count = group.Count();
                var severity = group.Sum(item => (int)item.Priority);
                var risk = Math.Min(1m, (count / 10m) + (severity / Math.Max(1m, count * 12m)));
                var category = group.GroupBy(item => item.Category).OrderByDescending(item => item.Count()).First().Key;
                var level = risk >= 0.75m ? "Critical" : risk >= 0.50m ? "High" : risk >= 0.25m ? "Medium" : "Low";
                return new HotspotResponse(group.Key.Lat, group.Key.Lon, count, Math.Round(risk, 3), category, level);
            }).OrderByDescending(item => item.RiskScore).Take(100).ToList();
    }

    public async Task<AiMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.AiTriagedAt != null, cancellationToken);
        var pending = await _db.AiTriageAnalyses.AsNoTracking().CountAsync(entity => entity.RequiresManualReview && entity.ReviewedAt == null, cancellationToken);
        var fraud = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.Status == ComplaintStatus.FraudReview, cancellationToken);
        var duplicates = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.Status == ComplaintStatus.Merged && entity.DuplicateOfComplaintId != null, cancellationToken);
        var confidence = await _db.AiTriageAnalyses.AsNoTracking().AverageAsync(entity => (decimal?)entity.ClassificationConfidence, cancellationToken) ?? 0m;
        return new AiMetricsResponse(total, pending, fraud, duplicates, Math.Round(confidence, 4), _options.UseGemini ? "Gemini" : "RuleBased", _options.UseGemini ? _options.Model : "civichero-rules-v1");
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableBackgroundTriage) return 0;
        var ids = await _db.Complaints.AsNoTracking().Where(entity => entity.Status == ComplaintStatus.Created && entity.AiTriagedAt == null)
            .OrderBy(entity => entity.CreatedAt).Take(Math.Clamp(_options.BatchSize, 1, 100)).Select(entity => entity.Id).ToListAsync(cancellationToken);
        var processed = 0;
        foreach (var id in ids)
        {
            try { await AnalyzeComplaintAsync(id, false, cancellationToken); processed++; }
            catch when (!cancellationToken.IsCancellationRequested) { _db.ChangeTracker.Clear(); }
        }
        return processed;
    }

    private async Task<FraudCheckResponse> BuildFraudResponseAsync(AiTextRequest request, AiProviderResult result, CancellationToken cancellationToken)
    {
        var signals = new List<string>();
        var score = result.FraudScore;
        if (request.CitizenId.HasValue)
        {
            var since = DateTimeOffset.UtcNow.AddHours(-Math.Clamp(_options.RecentSubmissionWindowHours, 1, 168));
            var recent = await _db.Complaints.AsNoTracking().CountAsync(entity => entity.CitizenId == request.CitizenId && entity.CreatedAt >= since, cancellationToken);
            if (recent >= _options.ExcessiveSubmissionCount)
            {
                score = Math.Min(1m, score + 0.30m);
                signals.Add($"Citizen submitted {recent} complaints in the configured review window.");
            }
        }
        if (result.FraudScore >= 0.45m) signals.Add(result.Reasoning);
        if (signals.Count == 0) signals.Add("No strong spam or abuse signal was detected.");
        var verdict = score >= 0.75m ? "HighRisk" : score >= _options.FraudReviewThreshold ? "RiskFlag" : "Safe";
        return new FraudCheckResponse(verdict, Math.Round(score, 4), score >= _options.FraudReviewThreshold, signals, result.Provider);
    }

    private async Task<AiProviderResult> ProviderAnalysisAsync(AiTextRequest request, CancellationToken cancellationToken)
        => await _gemini.AnalyzeAsync(request.Title, request.Description, request.Category, cancellationToken)
            ?? await _rules.AnalyzeAsync(request.Title, request.Description, request.Category, cancellationToken)
            ?? throw new InvalidOperationException("No AI provider is available.");

    private async Task<Department?> FindDepartmentAsync(string category, CancellationToken cancellationToken)
    {
        var keywords = category switch
        {
            "Pothole" or "Road Damage" => new[] { "road", "public works", "PWD" },
            "Garbage" or "Illegal Dumping" => new[] { "sanitation", "solid waste", "garbage" },
            "Streetlight" => new[] { "electric", "streetlight", "lighting" },
            "Water Leakage" => new[] { "water", "hydraulic" },
            "Drainage" => new[] { "drainage", "sewer" },
            "Public Safety" => new[] { "safety", "emergency" },
            _ => Array.Empty<string>()
        };
        var departments = await _db.Departments.AsNoTracking().Where(entity => entity.IsActive).ToListAsync(cancellationToken);
        return departments.FirstOrDefault(entity => keywords.Any(keyword => entity.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || entity.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    private static AiTriageResponse Map(Complaint complaint, AiTriageAnalysis analysis) => new(
        complaint.Id, complaint.Status.ToString(),
        new ClassificationResponse(analysis.PredictedCategory, analysis.PredictedDepartmentId, null, analysis.ClassificationConfidence, analysis.Provider, analysis.Reasoning),
        new DuplicateCheckResponse(analysis.DuplicateStatus, analysis.DuplicateScore, analysis.DuplicateComplaintId, [], analysis.Reasoning),
        new FraudCheckResponse(analysis.FraudVerdict, analysis.FraudScore, analysis.RequiresManualReview, [analysis.Reasoning], analysis.Provider),
        new PriorityPredictionResponse(analysis.PredictedPriority.ToString(), analysis.PriorityScore, [analysis.Reasoning], analysis.Provider),
        analysis.RequiresManualReview, analysis.AnalyzedAt);

    private static decimal TextSimilarity(string left, string right)
    {
        var a = RuleBasedAiService.Tokenize(left).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var b = RuleBasedAiService.Tokenize(right).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (a.Count == 0 || b.Count == 0) return 0m;
        var intersection = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
        return (2m * intersection) / (a.Count + b.Count);
    }

    private static double HaversineMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadius = 6371000d;
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
