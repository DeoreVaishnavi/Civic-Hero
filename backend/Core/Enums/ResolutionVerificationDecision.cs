namespace CivicHero.Backend.Core.Enums;

/// <summary>
/// Possible outcomes of resolution verification analysis.
/// </summary>
public enum ResolutionVerificationDecision
{
    /// <summary>
    /// The resolution image shows clear evidence that the issue has been resolved.
    /// </summary>
    LikelyResolved = 1,

    /// <summary>
    /// The resolution image shows clear evidence that the issue has NOT been resolved.
    /// </summary>
    LikelyNotResolved = 2,

    /// <summary>
    /// The analysis is inconclusive and requires manual review.
    /// </summary>
    ManualReviewRequired = 3,

    /// <summary>
    /// The analysis failed due to technical issues (e.g., unable to process image).
    /// </summary>
    AnalysisFailed = 4
}