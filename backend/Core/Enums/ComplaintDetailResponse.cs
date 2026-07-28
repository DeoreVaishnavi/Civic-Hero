using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public class ComplaintDetailResponse
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

    public ComplaintPriority Priority { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<TimelineRowDto> Timeline { get; set; } = new();

    public int VoteCount { get; set; }
}