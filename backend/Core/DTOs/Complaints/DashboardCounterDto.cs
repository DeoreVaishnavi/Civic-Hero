namespace CivicHero.Backend.Core.DTOs.Complaints;

public class DashboardCounterDto
{
    public int OpenCount { get; set; }

    public int ResolvedCount { get; set; }

    public int InProgressCount { get; set; }

    public int RejectedCount { get; set; }

    public int TotalCount { get; set; }
}