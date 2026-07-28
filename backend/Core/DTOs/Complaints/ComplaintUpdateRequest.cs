using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public class ComplaintUpdateRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int WardId { get; set; }

    public int DepartmentId { get; set; }

    public ComplaintPriority Priority { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? ResolutionNotes { get; set; }
}
