namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class AddProgressRequest
{
    public string Message { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset? EstimatedCompletionAt { get; set; }
}
