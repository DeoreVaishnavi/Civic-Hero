namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class BulkAssignRequest
{
    public List<long> ComplaintIds { get; set; } = new();
    public long OfficerId { get; set; }
    public string? Reason { get; set; }
}
