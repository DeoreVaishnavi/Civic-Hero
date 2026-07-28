using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.DTOs.Complaints;

public class StatusUpdateRequest
{
    public ComplaintStatus Status { get; set; }

    public string? Notes { get; set; }
}
