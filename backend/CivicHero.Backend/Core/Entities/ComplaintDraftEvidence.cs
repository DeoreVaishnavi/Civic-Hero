using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintDraftEvidence : BaseEntity
{
    public long ComplaintDraftId { get; set; }
    public string S3Key { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }

    public ComplaintDraft ComplaintDraft { get; set; } = null!;
}
