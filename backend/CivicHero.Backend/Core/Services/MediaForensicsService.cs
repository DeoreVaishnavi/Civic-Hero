using System.Security.Cryptography;
using System.Text.Json;
using CivicHero.Backend.Core.DTOs.Ai;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Core.Services;

public sealed class MediaForensicsService : IMediaForensicsService
{
    private const int MaxImagesPerComplaint = 20;
    private const int MaxReuseCandidates = 300;
    private const long MaxAnalyzedFileSize = 12L * 1024 * 1024;
    private const decimal ModerateGpsDistanceMeters = 250m;
    private const decimal HighGpsDistanceMeters = 1000m;

    private readonly CivicDbContext _db;
    private readonly IStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public MediaForensicsService(
        CivicDbContext db,
        IStorageService storage,
        ICurrentUserService currentUser)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<MediaForensicsReportResponse> AnalyzeComplaintMediaAsync(
        long complaintId,
        CancellationToken cancellationToken = default)
    {
        var complaint = await _db.Complaints.AsNoTracking()
            .Include(entity => entity.Images)
            .FirstOrDefaultAsync(entity => entity.Id == complaintId && !entity.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Complaint was not found.");

        EnsureScope(complaint);

        var sourceImages = complaint.Images
            .OrderBy(entity => entity.UploadedAt)
            .Take(MaxImagesPerComplaint)
            .ToArray();

        var analyzed = new List<AnalyzedImage>(sourceImages.Length);
        foreach (var image in sourceImages)
            analyzed.Add(await AnalyzeImageAsync(complaint, image, cancellationToken));

        await DetectExactReuseAsync(complaint, analyzed, cancellationToken);
        DetectEvidenceSequence(analyzed);

        var responses = analyzed.Select(MapImage).ToArray();
        var high = responses.Sum(image => image.Signals.Count(signal => signal.Severity == "High"));
        var medium = responses.Sum(image => image.Signals.Count(signal => signal.Severity == "Medium"));
        var score = Math.Round(Math.Min(1m, (high * 0.25m) + (medium * 0.12m)), 3);
        var verdict = high >= 2 || score >= 0.65m ? "HighRisk"
            : high > 0 || medium > 0 || score >= 0.25m ? "NeedsHumanReview"
            : "NoMaterialAnomalyDetected";

        var summary = BuildSummary(complaint.Images.Count, responses, high, medium);
        var analyzedAt = DateTimeOffset.UtcNow;
        AddAudit(complaint, responses.Length, high, medium, score, verdict, analyzedAt);
        await _db.SaveChangesAsync(cancellationToken);

        return new MediaForensicsReportResponse(
            complaint.Id,
            responses.Length,
            high,
            medium,
            score,
            verdict,
            summary,
            responses,
            analyzedAt);
    }

    private async Task<AnalyzedImage> AnalyzeImageAsync(
        Complaint complaint,
        ComplaintImage image,
        CancellationToken cancellationToken)
    {
        var result = new AnalyzedImage(image);

        if (image.FileSize <= 0 || image.FileSize > MaxAnalyzedFileSize)
        {
            result.Signals.Add(new MediaForensicsSignalResponse(
                "FILE_SIZE_NOT_ANALYZED",
                "Medium",
                $"The file size ({image.FileSize} bytes) is outside the safe on-demand analysis limit."));
            return result;
        }

        var download = await _storage.DownloadAsync(image.S3Key, image.FileName, cancellationToken);
        if (download is null)
        {
            result.Signals.Add(new MediaForensicsSignalResponse(
                "MEDIA_OBJECT_MISSING",
                "Medium",
                "The evidence metadata exists, but the stored media object could not be downloaded."));
            return result;
        }

        await using var content = download.Content;
        using var memory = new MemoryStream(image.FileSize > int.MaxValue ? 0 : (int)image.FileSize);
        await content.CopyToAsync(memory, cancellationToken);
        if (memory.Length > MaxAnalyzedFileSize)
        {
            result.Signals.Add(new MediaForensicsSignalResponse(
                "FILE_SIZE_NOT_ANALYZED",
                "Medium",
                "The downloaded file exceeded the safe on-demand analysis limit."));
            return result;
        }

        var bytes = memory.ToArray();
        result.Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        result.Metadata = MediaMetadataExtractor.Extract(bytes, image.MimeType);

        foreach (var note in result.Metadata.Notes)
            result.Signals.Add(new MediaForensicsSignalResponse("METADATA_NOTE", "Information", note));

        if (result.Metadata.Status is "NoExifMetadata" or "TextMetadataOnly")
            result.Signals.Add(new MediaForensicsSignalResponse(
                "EXIF_NOT_AVAILABLE",
                "Information",
                "No reliable EXIF camera/GPS block was available. Missing EXIF alone is not evidence of fraud."));

        if (result.Metadata.EditingTools.Count > 0)
            result.Signals.Add(new MediaForensicsSignalResponse(
                "EDITOR_METADATA_PRESENT",
                "Medium",
                $"Metadata references an editing application: {string.Join(", ", result.Metadata.EditingTools)}. Editing may be legitimate and requires human review."));

        AddGpsSignals(complaint, result);
        AddTimestampSignals(complaint, result);
        return result;
    }

    private async Task DetectExactReuseAsync(
        Complaint complaint,
        IReadOnlyList<AnalyzedImage> analyzed,
        CancellationToken cancellationToken)
    {
        var hashGroups = analyzed
            .Where(item => !string.IsNullOrWhiteSpace(item.Sha256))
            .GroupBy(item => item.Sha256, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in hashGroups)
        {
            foreach (var item in group)
                item.Signals.Add(new MediaForensicsSignalResponse(
                    "DUPLICATE_MEDIA_IN_COMPLAINT",
                    "Medium",
                    "This exact media file appears more than once in the same complaint."));
        }

        var knownHashes = analyzed
            .Where(item => !string.IsNullOrWhiteSpace(item.Sha256))
            .GroupBy(item => item.Sha256, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        if (knownHashes.Count == 0) return;

        var sizes = analyzed.Select(item => item.Image.FileSize).Where(size => size > 0).Distinct().ToArray();
        var mimeTypes = analyzed.Select(item => item.Image.MimeType).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();

        var candidates = _db.ComplaintImages.AsNoTracking()
            .Where(image => image.ComplaintId != complaint.Id && !image.Complaint.IsDeleted
                && sizes.Contains(image.FileSize) && mimeTypes.Contains(image.MimeType));

        if (IsSupervisor())
        {
            var departmentId = RequireSupervisorDepartment();
            var wardId = _currentUser.WardId;
            candidates = candidates.Where(image => image.Complaint.DepartmentId == departmentId && (!wardId.HasValue || image.Complaint.WardId == wardId.Value));
        }

        var candidateRows = await candidates
            .OrderByDescending(image => image.UploadedAt)
            .Take(MaxReuseCandidates)
            .Select(image => new ReuseCandidate(image.Id, image.ComplaintId, image.S3Key, image.FileName, image.FileSize, image.UploadedAt))
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidateRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.FileSize <= 0 || candidate.FileSize > MaxAnalyzedFileSize) continue;

            var download = await _storage.DownloadAsync(candidate.S3Key, candidate.FileName, cancellationToken);
            if (download is null) continue;
            await using var content = download.Content;
            using var memory = new MemoryStream(candidate.FileSize > int.MaxValue ? 0 : (int)candidate.FileSize);
            await content.CopyToAsync(memory, cancellationToken);
            if (memory.Length > MaxAnalyzedFileSize) continue;

            var hash = Convert.ToHexString(SHA256.HashData(memory.ToArray())).ToLowerInvariant();
            if (!knownHashes.TryGetValue(hash, out var matches)) continue;

            foreach (var match in matches)
                match.ReuseMatches.Add(new MediaReuseMatchResponse(
                    candidate.ComplaintId,
                    candidate.ImageId,
                    candidate.FileName,
                    candidate.UploadedAt));
        }

        foreach (var item in analyzed.Where(item => item.ReuseMatches.Count > 0))
            item.Signals.Add(new MediaForensicsSignalResponse(
                "EXACT_MEDIA_REUSED",
                "High",
                $"The exact file content matches evidence from {item.ReuseMatches.Count} other complaint image(s). Confirm context before taking action."));
    }

    private static void AddGpsSignals(Complaint complaint, AnalyzedImage image)
    {
        var metadata = image.Metadata;
        if (!metadata.Latitude.HasValue || !metadata.Longitude.HasValue) return;

        var distance = HaversineMeters(
            complaint.Latitude,
            complaint.Longitude,
            metadata.Latitude.Value,
            metadata.Longitude.Value);
        image.DistanceFromComplaintMeters = Math.Round((decimal)distance, 1);

        if (image.DistanceFromComplaintMeters > HighGpsDistanceMeters)
            image.Signals.Add(new MediaForensicsSignalResponse(
                "GPS_LOCATION_MISMATCH",
                "High",
                $"Photo GPS is approximately {image.DistanceFromComplaintMeters:0} metres from the complaint location."));
        else if (image.DistanceFromComplaintMeters > ModerateGpsDistanceMeters)
            image.Signals.Add(new MediaForensicsSignalResponse(
                "GPS_LOCATION_DIFFERENCE",
                "Medium",
                $"Photo GPS is approximately {image.DistanceFromComplaintMeters:0} metres from the complaint location."));
        else
            image.Signals.Add(new MediaForensicsSignalResponse(
                "GPS_LOCATION_CONSISTENT",
                "Information",
                $"Photo GPS is within approximately {image.DistanceFromComplaintMeters:0} metres of the complaint location."));
    }

    private static void AddTimestampSignals(Complaint complaint, AnalyzedImage image)
    {
        var metadata = image.Metadata;
        if (!metadata.CapturedAt.HasValue) return;

        var capture = metadata.CapturedAt.Value;
        var tolerance = metadata.CaptureTimeHasOffset ? TimeSpan.FromMinutes(20) : TimeSpan.FromHours(14);
        var upload = image.Image.UploadedAt;

        if (capture > upload.Add(tolerance))
            image.Signals.Add(new MediaForensicsSignalResponse(
                "CAPTURE_AFTER_UPLOAD",
                "High",
                "The embedded capture time is later than the upload time, beyond the allowed timezone/clock tolerance."));

        if (image.Image.IsResolutionEvidence && capture.Add(tolerance) < complaint.CreatedAt)
            image.Signals.Add(new MediaForensicsSignalResponse(
                "RESOLUTION_CAPTURE_BEFORE_COMPLAINT",
                "High",
                "Resolution evidence appears to have been captured before the complaint was created."));
        else if (!image.Image.IsResolutionEvidence && capture.AddYears(1).Add(tolerance) < complaint.CreatedAt)
            image.Signals.Add(new MediaForensicsSignalResponse(
                "OLD_INITIAL_EVIDENCE",
                "Medium",
                "Initial evidence appears to be more than one year older than the complaint."));

        if (upload - capture > TimeSpan.FromDays(30) + tolerance)
            image.Signals.Add(new MediaForensicsSignalResponse(
                "LONG_CAPTURE_TO_UPLOAD_DELAY",
                "Medium",
                "The embedded capture time is more than 30 days before the upload time."));
    }

    private static void DetectEvidenceSequence(IReadOnlyList<AnalyzedImage> analyzed)
    {
        var before = analyzed
            .Where(item => !item.Image.IsResolutionEvidence && item.Metadata.CapturedAt.HasValue)
            .OrderByDescending(item => item.Metadata.CapturedAt)
            .FirstOrDefault();
        if (before is null) return;

        foreach (var after in analyzed.Where(item => item.Image.IsResolutionEvidence && item.Metadata.CapturedAt.HasValue))
        {
            var tolerance = before.Metadata.CaptureTimeHasOffset && after.Metadata.CaptureTimeHasOffset
                ? TimeSpan.FromMinutes(20)
                : TimeSpan.FromHours(14);
            if (after.Metadata.CapturedAt!.Value.Add(tolerance) < before.Metadata.CapturedAt!.Value)
                after.Signals.Add(new MediaForensicsSignalResponse(
                    "BEFORE_AFTER_SEQUENCE_INCONSISTENT",
                    "High",
                    "Resolution evidence appears to have been captured before the latest initial evidence."));
        }
    }

    private void EnsureScope(Complaint complaint)
    {
        if (_currentUser.Role is nameof(UserRole.Admin) or nameof(UserRole.SuperAdmin)) return;
        if (!IsSupervisor()) throw new UnauthorizedAccessException("Supervisor or administrator permission is required.");

        var departmentId = RequireSupervisorDepartment();
        if (complaint.DepartmentId != departmentId)
            throw new UnauthorizedAccessException("This complaint is outside the Supervisor's assigned department.");
        if (_currentUser.WardId.HasValue && complaint.WardId != _currentUser.WardId.Value)
            throw new UnauthorizedAccessException("This complaint is outside the Supervisor's assigned ward.");
    }

    private bool IsSupervisor() => string.Equals(_currentUser.Role, nameof(UserRole.Supervisor), StringComparison.OrdinalIgnoreCase);

    private long RequireSupervisorDepartment() => _currentUser.DepartmentId
        ?? throw new BusinessRuleViolationException("The Supervisor account must be assigned to a department before media review.");

    private void AddAudit(
        Complaint complaint,
        int imagesAnalyzed,
        int high,
        int medium,
        decimal score,
        string verdict,
        DateTimeOffset analyzedAt)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.Email,
            UserRole = _currentUser.Role,
            Action = "MEDIA_FORENSICS_ANALYZED",
            EntityName = nameof(Complaint),
            EntityId = complaint.Id.ToString(),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                imagesAnalyzed,
                highRiskSignals = high,
                mediumRiskSignals = medium,
                riskScore = score,
                verdict,
                advisoryOnly = true
            }),
            Severity = high > 0 ? "Warning" : "Information",
            Success = true,
            HttpStatusCode = 200,
            CreatedAt = analyzedAt
        });
    }

    private static IReadOnlyList<string> BuildSummary(
        int totalImages,
        IReadOnlyList<MediaForensicsImageResponse> images,
        int high,
        int medium)
    {
        var summary = new List<string>();
        if (totalImages == 0) summary.Add("No complaint evidence images are available for analysis.");
        else summary.Add($"Analyzed {images.Count} of {totalImages} evidence image(s).");
        if (totalImages > MaxImagesPerComplaint) summary.Add($"Only the first {MaxImagesPerComplaint} images were analyzed in this on-demand review.");

        var withGps = images.Count(image => image.Latitude.HasValue && image.Longitude.HasValue);
        var withCaptureTime = images.Count(image => image.CapturedAtRaw is not null);
        var reused = images.Count(image => image.ExactReuseDetected);
        var editorTagged = images.Count(image => image.Signals.Any(signal => signal.Code == "EDITOR_METADATA_PRESENT"));

        summary.Add($"GPS metadata was available for {withGps} image(s); capture time was available for {withCaptureTime} image(s).");
        if (reused > 0) summary.Add($"Exact content reuse was detected for {reused} image(s).");
        if (editorTagged > 0) summary.Add($"Editing-tool metadata was detected for {editorTagged} image(s).");
        summary.Add($"Detected {high} high-risk and {medium} medium-risk advisory signal(s). No automatic fraud decision was made.");
        return summary;
    }

    private static MediaForensicsImageResponse MapImage(AnalyzedImage item) =>
        new(
            item.Image.Id,
            item.Image.FileName,
            item.Image.IsResolutionEvidence ? "Resolution" : "Initial",
            item.Image.FileSize,
            item.Image.MimeType,
            item.Sha256,
            item.Metadata.Status,
            item.Metadata.CameraMake,
            item.Metadata.CameraModel,
            item.Metadata.Software,
            item.Metadata.CapturedAtRaw,
            item.Metadata.CapturedAt,
            item.Metadata.Latitude,
            item.Metadata.Longitude,
            item.DistanceFromComplaintMeters,
            item.ReuseMatches.Count > 0,
            item.ReuseMatches,
            item.Signals);

    private static double HaversineMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadius = 6371000d;
        var phi1 = (double)lat1 * Math.PI / 180d;
        var phi2 = (double)lat2 * Math.PI / 180d;
        var deltaPhi = ((double)lat2 - (double)lat1) * Math.PI / 180d;
        var deltaLambda = ((double)lon2 - (double)lon1) * Math.PI / 180d;
        var a = Math.Sin(deltaPhi / 2d) * Math.Sin(deltaPhi / 2d)
            + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(deltaLambda / 2d) * Math.Sin(deltaLambda / 2d);
        return earthRadius * 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
    }

    private sealed class AnalyzedImage
    {
        public AnalyzedImage(ComplaintImage image) => Image = image;
        public ComplaintImage Image { get; }
        public string Sha256 { get; set; } = string.Empty;
        public ExtractedMediaMetadata Metadata { get; set; } = new("NotAnalyzed", null, null, null, null, null, false, null, null, [], []);
        public decimal? DistanceFromComplaintMeters { get; set; }
        public List<MediaReuseMatchResponse> ReuseMatches { get; } = [];
        public List<MediaForensicsSignalResponse> Signals { get; } = [];
    }

    private sealed record ReuseCandidate(
        long ImageId,
        long ComplaintId,
        string S3Key,
        string FileName,
        long FileSize,
        DateTimeOffset UploadedAt);
}
