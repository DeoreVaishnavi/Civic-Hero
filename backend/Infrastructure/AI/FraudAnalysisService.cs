using CivicHero.Backend.Core.DTOs.FraudDetection;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Service for analyzing complaints for fraud indicators using metadata, image hashing, and scoring models.
/// </summary>
public class FraudAnalysisService : IFraudAnalysisService
{
    private readonly CivicHeroDbContext _context;
    private readonly IMetadataForensics _metadataForensics;
    private readonly IImageHashService _imageHashService;
    private readonly FraudScoreCalculator _fraudScoreCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="FraudAnalysisService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="metadataForensics">Service for analyzing image metadata.</param>
    /// <param name="imageHashService">Service for computing image hashes.</param>
    /// <param name="fraudScoreCalculator">Calculator for fraud scores based on multiple signals.</param>
    public FraudAnalysisService(
        CivicHeroDbContext context,
        IMetadataForensics metadataForensics,
        IImageHashService imageHashService,
        FraudScoreCalculator fraudScoreCalculator)
    {
        _context = context;
        _metadataForensics = metadataForensics;
        _imageHashService = imageHashService;
        _fraudScoreCalculator = fraudScoreCalculator;
    }

    /// <summary>
    /// Analyzes a complaint for fraud indicators.
    /// </summary>
    /// <param name="complaintId">The complaint identifier.</param>
    /// <param name="complaintText">The text description of the complaint.</param>
    /// <param name="imageBytes">Optional image bytes attached to the complaint for forensic analysis.</param>
    /// <param name="latitude">Latitude of the incident location (optional).</param>
    /// <param name="longitude">Longitude of the incident location (optional).</param>
    /// <param name="incidentTime">Time of the incident (optional).</param>
    /// <returns>Fraud analysis results.</returns>
    public async Task<AiFraudAnalysisDto> AnalyzeComplaintForFraud(int complaintId, string complaintText, byte[]? imageBytes = null, double? latitude = null, double? longitude = null, DateTime? incidentTime = null)
    {
        int fraudScore = 0;
        var reasons = new List<string>();
        FraudRiskLevel riskLevel = FraudRiskLevel.Low;

        // If we have an image, perform forensic analysis
        if (imageBytes != null && imageBytes.Length > 0)
        {
            // Calculate fraud score using our forensic signals
            var (score, reasonList) = await _fraudScoreCalculator.ComputeFraudScoreAsync(
                imageBytes: imageBytes,
                expectedTimestamp: incidentTime,
                imageGpsLatitude: latitude,   // Note: We are using the complaint's latitude/longitude as the image GPS?
                imageGpsLongitude: longitude, // In reality, we would extract GPS from the image, but for now we use the complaint location as a proxy.
                complaintLatitude: latitude,
                complaintLongitude: longitude,
                userTrustScore: 0.5 // Placeholder - in a real app, we would fetch the user's trust score from the user profile
            );

            fraudScore = score;
            reasons.AddRange(reasonList);
        }
        else
        {
            // If no image, we can still assign a base score based on other factors?
            // For now, we'll set a low score and note that no image was provided.
            fraudScore = 10;
            reasons.Add("No image provided for forensic analysis.");
        }

        // Determine risk level based on score
        if (fraudScore >= 80) riskLevel = FraudRiskLevel.Critical;
        else if (fraudScore >= 60) riskLevel = FraudRiskLevel.High;
        else if (fraudScore >= 40) riskLevel = FraudRiskLevel.Medium;
        else riskLevel = FraudRiskLevel.Low;

        // Create and save the analysis entity
        var analysis = new AiFraudAnalysis
        {
            ComplaintId = complaintId,
            FraudScore = fraudScore,
            RiskLevel = riskLevel,
            Reasons = string.Join("; ", reasons),
            Reviewed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.AiFraudAnalyses.Add(analysis);
        await _context.SaveChangesAsync();

        // Return the DTO
        return new AiFraudAnalysisDto
        {
            Id = analysis.id,
            ComplaintId = analysis.ComplaintId,
            FraudScore = analysis.FraudScore,
            RiskLevel = analysis.RiskLevel,
            Reasons = analysis.Reasons,
            Reviewed = analysis.Reviewed,
            CreatedAt = analysis.CreatedAt
        };
    }

    /// <summary>
    /// Retrieves the fraud analysis for a complaint by its identifier.
    /// </summary>
    /// <param name="complaintId">The complaint identifier.</param>
    /// <returns>The fraud analysis if found; otherwise null.</returns>
    public async Task<AiFraudAnalysisDto?> GetFraudAnalysisByComplaintId(int complaintId)
    {
        var analysis = await _context.AiFraudAnalyses
            .FirstOrDefaultAsync(f => f.ComplaintId == complaintId);

        if (analysis == null)
            return null;

        return new AiFraudAnalysisDto
        {
            Id = analysis.id,
            ComplaintId = analysis.ComplaintId,
            FraudScore = analysis.FraudScore,
            RiskLevel = analysis.RiskLevel,
            Reasons = analysis.Reasons,
            Reviewed = analysis.Reviewed,
            CreatedAt = analysis.CreatedAt
        };
    }

    /// <summary>
    /// Marks a fraud analysis as reviewed.
    /// </summary>
    /// <param name="analysisId">The analysis identifier.</param>
    /// <returns>True if the analysis was found and marked; otherwise false.</returns>
    public async Task<bool> MarkAsReviewed(int analysisId)
    {
        var analysis = await _context.AiFraudAnalyses.FindAsync(analysisId);
        if (analysis == null)
            return false;

        analysis.Reviewed = true;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Calculates the average fraud score across all analyses.
    /// </summary>
    public async Task<double> CalculateAverageFraudScore()
    {
        var average = await _context.AiFraudAnalyses
            .AverageAsync(f => (double?)f.FraudScore);

        return average ?? 0;
    }
}