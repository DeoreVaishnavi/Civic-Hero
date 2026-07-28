using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public class ComplaintQueryParameters
{
    public ComplaintStatus? Status { get; set; }

    public ComplaintPriority? Priority { get; set; }

    public int? WardId { get; set; }

    public int? DepartmentId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}