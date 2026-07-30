namespace CivicHero.Core.Enums;

/// <summary>
/// Types of evidence images for fraud analysis
/// </summary>
public enum EvidenceImageType
{
    /// <summary>
    /// Image submitted with the initial complaint
    /// </summary>
    Complaint = 1,

    /// <summary>
    /// Image submitted as part of the resolution verification
    /// </summary>
    Resolution = 2
}