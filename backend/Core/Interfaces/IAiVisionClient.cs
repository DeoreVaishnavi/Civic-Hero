using CivicHero.Backend.Core.DTOs.AI;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Client for communicating with the AI vision microservice.
/// </summary>
public interface IAiVisionClient
{
    /// <summary>
    /// Compares a complaint image with a resolution image to determine if the issue appears resolved.
    /// </summary>
    /// <param name="complaintImageBytes">The bytes of the complaint image.</param>
    /// <param name="resolutionImageBytes">The bytes of the resolution image.</param>
    /// <param name="complaintDescription">Optional description of the complaint for context.</param>
    /// <returns>The vision analysis result.</returns>
    Task<VisionComparisonResult> CompareImagesAsync(byte[] complaintImageBytes, byte[] resolutionImageBytes, string? complaintDescription = null);
}