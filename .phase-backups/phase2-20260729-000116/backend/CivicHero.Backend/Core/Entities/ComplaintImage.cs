using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintImage : BaseEntity
{
    public long ComplaintId { get; set; }
    public string S3Key { get; set; } = string.Empty;
    public string? S3Url { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public bool IsResolutionEvidence { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
}
