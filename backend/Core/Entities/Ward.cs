using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class Ward : SoftDeleteEntity
{
    public long DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal BoundaryNorth { get; set; }
    public decimal BoundarySouth { get; set; }
    public decimal BoundaryEast { get; set; }
    public decimal BoundaryWest { get; set; }
    public bool IsActive { get; set; } = true;

    public Department Department { get; set; } = null!;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
