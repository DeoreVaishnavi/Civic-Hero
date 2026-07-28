namespace CivicHero.Backend.Core.DTOs.Complaints;

public class TimelineNoteCreateDto
{
    public string Notes { get; set; } = string.Empty;

    public int? PerformedBy { get; set; }
}