using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class Department : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    public ICollection<Ward> Wards { get; set; } = new List<Ward>();
    public ICollection<Contractor> Contractors { get; set; } = new List<Contractor>();
}
