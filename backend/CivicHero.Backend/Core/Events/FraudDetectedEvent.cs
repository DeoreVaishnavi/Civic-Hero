using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Events;

/// <summary>
/// Raised when AI detects a potentially fraudulent complaint.
/// </summary>
public sealed class FraudDetectedEvent : DomainEvent
{
    /// <summary>
    /// Complaint identifier.
    /// </summary>
    public Guid ComplaintId { get; }

    /// <summary>
    /// AI analysis identifier.
    /// </summary>
    public Guid AnalysisId { get; }

    /// <summary>
    /// AI confidence score.
    /// Value ranges from 0.00 to 1.00.
    /// </summary>
    public decimal ConfidenceScore { get; }

    /// <summary>
    /// AI model name.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Summary explaining why fraud was detected.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Creates a new fraud detected event.
    /// </summary>
    public FraudDetectedEvent(
        Guid complaintId,
        Guid analysisId,
        decimal confidenceScore,
        string modelName,
        string reason)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (analysisId == Guid.Empty)
            throw new ArgumentException("Analysis ID is required.", nameof(analysisId));

        if (confidenceScore < 0m || confidenceScore > 1m)
            throw new ArgumentOutOfRangeException(
                nameof(confidenceScore),
                "Confidence score must be between 0.00 and 1.00.");

        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name is required.", nameof(modelName));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        ComplaintId = complaintId;
        AnalysisId = analysisId;
        ConfidenceScore = confidenceScore;
        ModelName = modelName.Trim();
        Reason = reason.Trim();
    }
}