namespace CivicHero.Backend.Core.DTOs.Users;

public sealed class UserDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
    public bool IsEmailVerified { get; set; }
}
