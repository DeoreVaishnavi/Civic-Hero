using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

public sealed class Contractor : AuditableEntity
{
    public long DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ContractorStatus Status { get; set; } = ContractorStatus.Active;
}
