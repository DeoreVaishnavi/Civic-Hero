namespace CivicHero.Backend.Core.DTOs.Users;

public sealed class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
}
