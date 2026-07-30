namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class AssignComplaintRequest
{
    public long ComplaintId { get; set; }
    public long OfficerId { get; set; }
    public string? Reason { get; set; }
}
