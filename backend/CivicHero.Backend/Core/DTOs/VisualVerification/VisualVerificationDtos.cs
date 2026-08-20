namespace CivicHero.Backend.Core.DTOs.VisualVerification;

public sealed class VisualVerificationQuery
{
    public bool ReviewRequiredOnly { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class VisualVerificationHumanDecisionRequest
{
    public string Decision { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class VisualVerificationResponse
{
    public long Id { get; init; }
    public long ComplaintId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string ComplaintTitle { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string WardName { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Verdict { get; init; } = string.Empty;
    public decimal CompletionScore { get; init; }
    public decimal ImageQualityScore { get; init; }
    public decimal ManipulationRiskScore { get; init; }
    public decimal OverallConfidence { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public IReadOnlyList<string> Observations { get; init; } = Array.Empty<string>();
    public string EvidenceFingerprint { get; init; } = string.Empty;
    public IReadOnlyList<VisualEvidenceImageResponse> BeforeImages { get; init; } = Array.Empty<VisualEvidenceImageResponse>();
    public IReadOnlyList<VisualEvidenceImageResponse> AfterImages { get; init; } = Array.Empty<VisualEvidenceImageResponse>();
    public bool RequiresHumanReview { get; init; }
    public string? HumanDecision { get; init; }
    public string? HumanNotes { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
}

public sealed class VisualEvidenceImageResponse
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public string DownloadPath { get; init; } = string.Empty;
}

public sealed record VisualProviderResult(
    string Provider,
    string Model,
    string Verdict,
    decimal CompletionScore,
    decimal ImageQualityScore,
    decimal ManipulationRiskScore,
    decimal OverallConfidence,
    string Reasoning,
    IReadOnlyList<string> Observations,
    string? RawResponse = null);
