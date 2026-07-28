namespace CivicHero.Backend.Core.Entities;

public class ComplaintTimeline
{
    public int Id { get; set; }

    public int ComplaintId { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public Complaint Complaint { get; set; } = null!;
}
