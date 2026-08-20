using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

public sealed class SystemSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "String";
    public string? Description { get; set; }
    public string Group { get; set; } = "General";
    public bool IsPublic { get; set; }
    public bool IsSensitive { get; set; }
    public long? UpdatedByUserId { get; set; }
}
