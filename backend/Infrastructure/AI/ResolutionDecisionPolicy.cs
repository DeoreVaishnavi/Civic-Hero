using System;
using System.Collections.Generic;
using CivicHero.Backend.Core.DTOs.AI;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Policy for mapping raw vision analysis scores to final resolution verification decisions.
/// This allows tuning of thresholds without changing the core logic.
/// </summary>
public class ResolutionDecisionPolicy
{
    /// <summary>
    /// Threshold for considering images similar enough that the issue might still be visible.
    /// </summary>
    public double SimilarityThresholdHigh { get; set; } = 0.8;

    /// <summary>
    /// Threshold for considering images different enough that the issue might be resolved.
    /// </summary>
    public double SimilarityThresholdLow { get; set; } = 0.4;

    /// <summary>
    /// Minimum confidence required for a LikelyResolved or LikelyNotResolved decision.
    /// Below this, results fall back to ManualReviewRequired.
    /// </summary>
    public double MinConfidenceForDecision { get; set; } = 0.65;

    /// <summary>
    /// Adjusts how strongly similarity affects the issue visibility determination.
    /// </summary>
    public double SimilarityImpactFactor { get; set; } = 0.7;

    /// <summary>
    /// Determines the final resolution verification decision based on the vision analysis result.
    /// </summary>
    /// <param name="analysis">The raw vision analysis result.</param>
    /// <returns>The final decision after applying policy rules.</returns>
    public ResolutionVerificationDecision DetermineDecision(VisionComparisonResult analysis)
    {
        // If the analysis already indicates failure, respect that
        if (analysis.Decision == ResolutionVerificationDecision.AnalysisFailed)
        {
            return ResolutionVerificationDecision.AnalysisFailed;
        }

        // Apply confidence threshold
        if (analysis.ConfidenceScore < MinConfidenceForDecision)
        {
            return ResolutionVerificationDecision.ManualReviewRequired;
        }

        // If the vision service already made a clear decision, we might still adjust based on policy
        // but we'll respect its judgment if confidence is high enough
        if (analysis.ConfidenceScore >= 0.8)
        {
            return analysis.Decision;
        }

        // Otherwise, apply our policy logic based on similarity score
        double similarity = analysis.SimilarityScore;

        // High similarity suggests the scene hasn't changed much (issue may still be present)
        if (similarity >= SimilarityThresholdHigh)
        {
            return ResolutionVerificationDecision.LikelyNotResolved;
        }
        // Low similarity suggests significant change (issue may be resolved)
        else if (similarity <= SimilarityThresholdLow)
        {
            return ResolutionVerificationDecision.LikelyResolved;
        }
        // Middle range is uncertain
        else
        {
            return ResolutionVerificationDecision.ManualReviewRequired;
        }
    }

    /// <summary>
    /// Determines whether the issue is still visible based on the analysis and policy.
    /// </summary>
    /// <param name="analysis">The vision analysis result.</param>
    /// <returns>True if the issue is likely still visible, false otherwise.</returns>
    public bool DetermineIssueVisibility(VisionComparisonResult analysis)
    {
        // If analysis failed, we default to false (assume not visible to avoid false positives)
        if (analysis.Decision == ResolutionVerificationDecision.AnalysisFailed)
        {
            return false;
        }

        // High confidence in the vision service's visibility assessment
        if (analysis.ConfidenceScore >= 0.8)
        {
            return analysis.IssueStillVisible;
        }

        // Otherwise, use similarity as a proxy: high similarity = issue likely still visible
        double similarity = analysis.SimilarityScore;
        return similarity >= SimilarityThresholdHigh;
    }
}