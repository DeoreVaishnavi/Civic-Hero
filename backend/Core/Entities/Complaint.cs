using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public class Complaint
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CitizenId { get; set; }

    public int WardId { get; set; }

    public int DepartmentId { get; set; }

    public int? AssignedOfficerId { get; set; }

    public int? AssignedContractorId { get; set; }

    public ComplaintStatus Status { get; set; }
        = ComplaintStatus.Submitted;

    public ComplaintPriority Priority { get; set; }
        = ComplaintPriority.Medium;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; }
        = DateTime.UtcNow;

    public ICollection<ComplaintTimeline> Timeline { get; set; }
        = new List<ComplaintTimeline>();
}