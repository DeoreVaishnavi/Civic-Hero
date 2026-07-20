using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents an image attached to a complaint.
/// Stores only file metadata.
/// </summary>
public sealed class ComplaintImage : AuditableEntity
{
    public Guid ComplaintId { get; private set; }

    public Complaint Complaint { get; private set; } = null!;

    /// <summary>
    /// Original filename uploaded by the user.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Unique filename used in storage.
    /// </summary>
    public string StoredFileName { get; private set; }

    /// <summary>
    /// Relative or absolute storage path.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// MIME type.
    /// Example: image/jpeg
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// Indicates whether AI metadata analysis has completed.
    /// </summary>
    public bool MetadataAnalyzed { get; private set; }

private ComplaintImage()
{
    Complaint = null!;

    FileName = string.Empty;
    StoredFileName = string.Empty;
    FilePath = string.Empty;
    ContentType = string.Empty;
}

    public ComplaintImage(
        Guid complaintId,
        string fileName,
        string storedFileName,
        string filePath,
        string contentType,
        long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(storedFileName))
            throw new ArgumentException("Stored file name is required.", nameof(storedFileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        if (fileSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize));

        ComplaintId = complaintId;
        FileName = fileName.Trim();
        StoredFileName = storedFileName.Trim();
        FilePath = filePath.Trim();
        ContentType = contentType.Trim();
        FileSize = fileSize;
    }

    public void MarkMetadataAnalyzed()
    {
        MetadataAnalyzed = true;
    }
}