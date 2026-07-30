using CivicHero.Backend.Core.DTOs.AI;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Service for verifying whether a resolution image shows that a reported issue has been resolved.
/// </summary>
public interface IResolutionVerificationService
{
    /// <summary>
    /// Analyzes a complaint image and a resolution image to determine if the issue has been resolved.
    /// </summary>
    /// <param name="request">The comparison request containing images and metadata.</param>
    /// <returns>The verification result.</returns>
    Task<VisionComparisonResult> VerifyResolutionAsync(VisionComparisonRequest request);

    /// <summary>
    /// Retrieves the verification result for a complaint by its identifier.
    /// </summary>
    /// <param name="complaintId">The complaint identifier.</param>
    /// <returns>The verification result if found; otherwise null.</returns>
    Task<VisionComparisonResult?> GetVerificationByComplaintId(int complaintId);

    /// <summary>
    /// Marks a verification result as reviewed.
    /// </summary>
    /// <param name="verificationId">The verification identifier.</param>
    /// <returns>True if the verification was found and marked; otherwise false.</returns>
    Task<bool> MarkAsReviewed(int verificationId);
}