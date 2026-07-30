using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class Contractor : SoftDeleteEntity
{
    public long DepartmentId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ContractorStatus Status { get; set; } = ContractorStatus.Pending;

    public Department Department { get; set; } = null!;
}
