using CivicHero.Backend.Core.DTOs.AI;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Stub implementation of the AI vision client for development and testing.
/// Returns mock responses to allow development without the Python microservice.
/// </summary>
public class StubAiVisionClient : IAiVisionClient
{
    /// <summary>
    /// Compares a complaint image with a resolution image to determine if the issue appears resolved.
    /// This stub implementation returns a mock response based on simple heuristics.
    /// </summary>
    /// <param name="complaintImageBytes">The bytes of the complaint image.</param>
    /// <param name="resolutionImageBytes">The bytes of the resolution image.</param>
    /// <param name="complaintDescription">Optional description of the complaint for context.</param>
    /// <returns>The vision analysis result.</returns>
    public Task<VisionComparisonResult> CompareImagesAsync(byte[] complaintImageBytes, byte[] resolutionImageBytes, string? complaintDescription = null)
    {
        return Task.FromResult(GenerateMockResult(complaintImageBytes, resolutionImageBytes, complaintDescription));
    }

    private VisionComparisonResult GenerateMockResult(byte[] complaintImageBytes, byte[] resolutionImageBytes, string? complaintDescription)
    {
        var result = new VisionComparisonResult
        {
            AnalysisDetails = "Stub analysis: Comparing complaint and resolution images.",
            Observations = new List<string> { "This is a mock response from the stub vision client." }
        };

        // Simple heuristic: if both images are provided and have similar size, assume similar
        // In a real implementation, this would use computer vision techniques
        if (complaintImageBytes != null && resolutionImageBytes != null)
        {
            // Very basic similarity check based on byte array length
            double sizeSimilarity = 1.0 - Math.Abs(complaintImageBytes.Length - resolutionImageBytes.Length) /
                                   (double)Math.Max(complaintImageBytes.Length, resolutionImageBytes.Length);

            // Simulate some randomness for demonstration
            var random = new Random();
            double similarityScore = 0.5 + (sizeSimilarity * 0.3) + (random.NextDouble() * 0.2);
            similarityScore = Math.Max(0.0, Math.Min(1.0, similarityScore)); // Clamp to 0-1

            result.SimilarityScore = similarityScore;

            // Determine if issue is still visible based on similarity
            // Higher similarity means more likely the issue is still there (images are similar)
            // Lower similarity means more likely the issue is resolved (images are different)
            if (similarityScore > 0.8)
            {
                result.Decision = ResolutionVerificationDecision.LikelyNotResolved;
                result.ConfidenceScore = 0.7;
                result.IssueStillVisible = true;
                result.AnalysisDetails += " Images appear very similar, suggesting the issue may not be resolved.";
                result.Observations.Add("High similarity between complaint and resolution images.");
            }
            else if (similarityScore < 0.4)
            {
                result.Decision = ResolutionVerificationDecision.LikelyResolved;
                result.ConfidenceScore = 0.8;
                result.IssueStillVisible = false;
                result.AnalysisDetails += " Images appear significantly different, suggesting the issue may be resolved.";
                result.Observations.Add("Low similarity between complaint and resolution images.");
            }
            else
            {
                result.Decision = ResolutionVerificationDecision.ManualReviewRequired;
                result.ConfidenceScore = 0.6;
                result.IssueStillVisible = false; // Could go either way
                result.AnalysisDetails += " Images show moderate similarity; manual review recommended.";
                result.Observations.Add("Moderate similarity between images - inconclusive result.");
            }
        }
        else
        {
            // If we don't have both images, we can't do a meaningful comparison
            result.Decision = ResolutionVerificationDecision.AnalysisFailed;
            result.ConfidenceScore = 0.0;
            result.IssueStillVisible = false;
            result.AnalysisDetails += " Unable to perform comparison: missing image data.";
            result.Observations.Add("Missing complaint or resolution image data.");
        }

        return result;
    }
}