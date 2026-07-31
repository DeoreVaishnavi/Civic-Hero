namespace CivicHero.Backend.Core.DTOs.Assignments;

public sealed class ReassignComplaintRequest
{
    public long OfficerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
