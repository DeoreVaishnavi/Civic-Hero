namespace CivicHero.Backend.Core.DTOs.Complaints;

public class TimelineRowDto
{
    public int Id { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}