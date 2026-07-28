namespace CivicHero.Backend.Core.DTOs.Complaints;

public class ComplaintSummaryResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;
}