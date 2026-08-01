using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintDraft : AuditableEntity
{
    public long CitizenId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string CitizenSeverity { get; set; } = "Medium";
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public bool PossibleEmergency { get; set; }
    public string? EmergencyReason { get; set; }

    public User Citizen { get; set; } = null!;
    public Department? Department { get; set; }
    public Ward? Ward { get; set; }
    public ICollection<ComplaintDraftEvidence> Evidence { get; set; } = new List<ComplaintDraftEvidence>();
}
