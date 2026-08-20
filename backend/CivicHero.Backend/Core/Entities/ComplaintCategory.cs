using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class ComplaintCategory : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultPriority { get; set; } = "Medium";
    public string? Icon { get; set; }
    public long? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Department? Department { get; set; }
}
