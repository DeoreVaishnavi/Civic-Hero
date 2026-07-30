namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Service for analyzing image metadata to detect potential fraud indicators.
/// </summary>
public interface IMetadataForensics
{
    /// <summary>
    /// Analyzes image metadata for signs of manipulation or inconsistency.
    /// </summary>
    /// <param name="imageBytes">The image bytes to analyze.</param>
    /// <param name="expectedTimestamp">Expected timestamp of the incident (optional).</param>
    /// <returns>Analysis results including confidence scores and detected issues.</returns>
    Task<ImageMetadataAnalysisResult> AnalyzeMetadataAsync(byte[] imageBytes, DateTime? expectedTimestamp = null);
}

/// <summary>
/// Result of image metadata analysis.
/// </summary>
public class ImageMetadataAnalysisResult
{
    /// <summary>
    /// Overall confidence that the metadata is authentic (0.0 to 1.0).
    /// </summary>
    public double AuthenticityScore { get; set; }

    /// <summary>
    /// Indicates if the image contains EXIF data.
    /// </summary>
    public bool HasExifData { get; set; }

    /// <summary>
    /// The software that likely created or modified the image, if detectable.
    /// </summary>
    public string? Software { get; set; }

    /// <summary>
    /// The date/time the image was originally created, if available.
    /// </summary>
    public DateTimeOffset? DateTimeOriginal { get; set; }

    /// <summary>
    /// The date/time the image was digitized, if available.
    /// </summary>
    public DateTimeOffset? DateTimeDigitized { get; set; }

    /// <summary>
    /// The date/time the file was last modified, if available.
    /// </summary>
    public DateTimeOffset? DateTimeModified { get; set; }

    /// <summary>
    /// List of warnings or inconsistencies found in the metadata.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Camera make, if available.
    /// </summary>
    public string? Make { get; set; }
}

/// <summary>
/// Camera model, if available.
/// </>