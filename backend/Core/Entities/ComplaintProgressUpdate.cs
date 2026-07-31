using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintProgressUpdate : BaseEntity
{
    public long ComplaintId { get; set; }
    public long OfficerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Complaint Complaint { get; set; } = null!;
    public User Officer { get; set; } = null!;
}
