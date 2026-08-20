namespace CivicHero.Backend.Core.DTOs.Ai;

public sealed record MediaForensicsSignalResponse(
    string Code,
    string Severity,
    string Message);

public sealed record MediaReuseMatchResponse(
    long ComplaintId,
    long ImageId,
    string FileName,
    DateTimeOffset UploadedAt);

public sealed record MediaForensicsImageResponse(
    long ImageId,
    string FileName,
    string EvidenceType,
    long FileSize,
    string MimeType,
    string Sha256,
    string MetadataStatus,
    string? CameraMake,
    string? CameraModel,
    string? Software,
    string? CapturedAtRaw,
    DateTimeOffset? CapturedAt,
    decimal? Latitude,
    decimal? Longitude,
    decimal? DistanceFromComplaintMeters,
    bool ExactReuseDetected,
    IReadOnlyList<MediaReuseMatchResponse> ReuseMatches,
    IReadOnlyList<MediaForensicsSignalResponse> Signals);

public sealed record MediaForensicsReportResponse(
    long ComplaintId,
    int ImagesAnalyzed,
    int HighRiskSignals,
    int MediumRiskSignals,
    decimal RiskScore,
    string Verdict,
    IReadOnlyList<string> Summary,
    IReadOnlyList<MediaForensicsImageResponse> Images,
    DateTimeOffset AnalyzedAt);
