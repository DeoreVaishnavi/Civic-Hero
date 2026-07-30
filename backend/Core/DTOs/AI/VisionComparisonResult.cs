using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.AI;

/// <summary>
/// Result of comparing a complaint image with a resolution image to verify if the issue has been resolved.
/// </summary>
public class VisionComparisonResult
{
    /// <summary>
    /// Decision on whether the issue appears to be resolved based on visual comparison.
    /// </summary>
    public ResolutionVerificationDecision Decision { get; set; }

    /// <summary>
    /// Confidence score for the decision (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Detailed explanation of the analysis performed.
    /// </summary>
    public string AnalysisDetails { get; set; } = string.Empty;

    /// <summary>
    /// Similarity score between the images (0.0 to 1.0), where higher means more similar.
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Indicates whether the specific issue mentioned in the complaint appears to be present in the resolution image.
    /// </summary>
    public bool IssueStillVisible { get; set; }

    /// <summary>
    /// List of observations made during the analysis.
    /// </summary>
    public List<string> Observations { get; set; } = new();
}