using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public class ComplaintCreateRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CitizenId { get; set; }

    public int WardId { get; set; }

    public int DepartmentId { get; set; }

    public ComplaintPriority Priority { get; set; }
        = ComplaintPriority.Medium;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Address { get; set; } = string.Empty;
}