using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents the result of analyzing whether a resolution image shows that a reported issue has been resolved.
/// </summary>
public class ResolutionVerification
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public ResolutionVerificationDecision Decision { get; set; }
    public double ConfidenceScore { get; set; } // 0.0 to 1.0
    public double SimilarityScore { get; set; } // 0.0 to 1.0, similarity between images
    public bool IssueStillVisible { get; set; } // Whether the issue is still visible in the resolution image
    public string AnalysisDetails { get; set; } = string.Empty; // JSON or text description of the analysis
    public bool Reviewed { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Complaint? Complaint { get; set; }
}