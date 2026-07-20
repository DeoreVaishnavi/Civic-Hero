using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents the result of an AI fraud analysis for a complaint.
/// Each analysis is immutable and stored as a historical record.
/// </summary>
public sealed class AiFraudAnalysis : AuditableEntity
{
    /// <summary>
    /// Related complaint identifier.
    /// </summary>
    public Guid ComplaintId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Complaint Complaint { get; private set; }

    /// <summary>
    /// AI model name.
    /// Example: Llama 3.1
    /// </summary>
    public string ModelName { get; private set; }

    /// <summary>
    /// AI model version.
    /// </summary>
    public string ModelVersion { get; private set; }

    /// <summary>
    /// Fraud confidence score (0.00 - 1.00).
    /// </summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>
    /// Indicates whether the complaint is suspected to be fraudulent.
    /// </summary>
    public bool IsFraudDetected { get; private set; }

    /// <summary>
    /// AI explanation of the analysis.
    /// </summary>
    public string AnalysisSummary { get; private set; }

    /// <summary>
    /// UTC timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedOnUtc { get; private set; }

    private AiFraudAnalysis()
    {
        Complaint = null!;
        ModelName = string.Empty;
        ModelVersion = string.Empty;
        AnalysisSummary = string.Empty;
    }

    public AiFraudAnalysis(
        Guid complaintId,
        string modelName,
        string modelVersion,
        decimal confidenceScore,
        bool isFraudDetected,
        string analysisSummary)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name is required.", nameof(modelName));

        if (string.IsNullOrWhiteSpace(modelVersion))
            throw new ArgumentException("Model version is required.", nameof(modelVersion));

        if (confidenceScore < 0m || confidenceScore > 1m)
            throw new ArgumentOutOfRangeException(
                nameof(confidenceScore),
                "Confidence score must be between 0.00 and 1.00.");

        if (string.IsNullOrWhiteSpace(analysisSummary))
            throw new ArgumentException("Analysis summary is required.", nameof(analysisSummary));

        ComplaintId = complaintId;
        ModelName = modelName.Trim();
        ModelVersion = modelVersion.Trim();
        ConfidenceScore = confidenceScore;
        IsFraudDetected = isFraudDetected;
        AnalysisSummary = analysisSummary.Trim();
        AnalyzedOnUtc = DateTime.UtcNow;
    }
}