using System;
using System.Threading.Tasks;
using CivicHero.Backend.Core.DTOs.AI;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Service for verifying whether a resolution image shows that a reported issue has been resolved.
/// Uses the AI vision microservice and applies business policy to make final decisions.
/// </summary>
public class ResolutionVerificationService : IResolutionVerificationService
{
    private readonly IAiVisionClient _visionClient;
    private readonly ResolutionDecisionPolicy _policy;
    private readonly CivicHeroDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolutionVerificationService"/> class.
    /// </summary>
    /// <param name="visionClient">Client for communicating with the AI vision microservice.</param>
    /// <param name="policy">Policy for mapping vision scores to final decisions.</param>
    /// <param name="context">The database context.</param>
    public ResolutionVerificationService(IAiVisionClient visionClient, ResolutionDecisionPolicy policy, CivicHeroDbContext context)
    {
        _visionClient = visionClient ?? throw new ArgumentNullException(nameof(visionClient));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Analyzes a complaint image and a resolution image to determine if the issue has been resolved.
    /// </summary>
    /// <param name="request">The comparison request containing images and metadata.</param>
    /// <returns>The verification result.</returns>
    public async Task<VisionComparisonResult> VerifyResolutionAsync(VisionComparisonRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Get raw analysis from the vision microservice
        VisionComparisonResult rawAnalysis = await _visionClient.CompareImagesAsync(
            request.ComplaintImageBytes,
            request.ResolutionImageBytes,
            request.ComplaintDescription);

        // Apply our policy to make the final decision
        rawAnalysis.Decision = _policy.DetermineDecision(rawAnalysis);

        // Update analysis details to reflect that policy was applied
        rawAnalysis.AnalysisDetails += " Applied resolution verification policy.";
        if (!rawAnalysis.Observations.Contains("Applied resolution verification policy."))
        {
            rawAnalysis.Observations.Add("Applied resolution verification policy.");
        }

        // Save the verification result to the database
        var verification = new ResolutionVerification
        {
            ComplaintId = request.ComplaintId,
            Decision = rawAnalysis.Decision,
            ConfidenceScore = rawAnalysis.ConfidenceScore,
            SimilarityScore = rawAnalysis.SimilarityScore,
            IssueStillVisible = rawAnalysis.IssueStillVisible,
            AnalysisDetails = rawAnalysis.AnalysisDetails,
            Reviewed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ResolutionVerifications.Add(verification);
        await _context.SaveChangesAsync();

        return rawAnalysis;
    }

    /// <summary>
    /// Retrieves the verification result for a complaint by its identifier.
    /// </summary>
    /// <param name="complaintId">The complaint identifier.</param>
    /// <returns>The verification result if found; otherwise null.</returns>
    public async Task<VisionComparisonResult?> GetVerificationByComplaintId(int complaintId)
    {
        var verification = await _context.ResolutionVerifications
            .Where(v => v.ComplaintId == complaintId)
            .OrderByDescending(v => v.CreatedAt) // Get the most recent one
            .FirstOrDefaultAsync();

        if (verification == null)
            return null;

        return new VisionComparisonResult
        {
            Decision = verification.Decision,
            ConfidenceScore = verification.ConfidenceScore,
            AnalysisDetails = verification.AnalysisDetails,
            SimilarityScore = verification.SimilarityScore,
            IssueStillVisible = verification.IssueStillVisible,
            Observations = new List<string> { $"Retrieved stored verification for complaint {complaintId}" }
        };
    }

    /// <summary>
    /// Marks a verification result as reviewed.
    /// </summary>
    /// <param name="verificationId">The verification identifier.</param>
    /// <returns>True if the verification was found and marked; otherwise false.</returns>
    public async Task<bool> MarkAsReviewed(int verificationId)
    {
        var verification = await _context.ResolutionVerifications.FindAsync(verificationId);
        if (verification == null)
            return false;

        verification.Reviewed = true;
        await _context.SaveChangesAsync();
        return true;
    }
}