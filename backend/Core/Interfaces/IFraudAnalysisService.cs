using CivicHero.Backend.Core.DTOs.FraudDetection;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Service for analyzing complaints for fraud indicators.
/// </summary>
public interface IFraudAnalysisService
{
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
    Task<AiFraudAnalysisDto> AnalyzeComplaintForFraud(int complaintId, string complaintText, byte[]? imageBytes = null, double? latitude = null, double? longitude = null, DateTime? incidentTime = null);

    /// <summary>
    /// Retrieves the fraud analysis for a complaint by its identifier.
    /// </summary>
    /// <param name="complaintId">The complaint identifier.</param>
    /// <returns>The fraud analysis if found; otherwise null.</returns>
    Task<AiFraudAnalysisDto?> GetFraudAnalysisByComplaintId(int complaintId);

    /// <summary>
    /// Marks a fraud analysis as reviewed.
    /// </summary>
    /// <param name="analysisId">The analysis identifier.</param>
    /// <returns>True if the analysis was found and marked; otherwise false.</returns>
    Task<bool> MarkAsReviewed(int analysisId);

    /// <summary>
    /// Calculates the average fraud score across all analyses.
    /// </Task>
    Task<double> CalculateAverageFraudScore();
}