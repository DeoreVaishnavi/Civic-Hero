using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public class ComplaintRowDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public ComplaintStatus Status { get; set; }

    public ComplaintPriority Priority { get; set; }

    public int WardId { get; set; }

    public int DepartmentId { get; set; }

    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}