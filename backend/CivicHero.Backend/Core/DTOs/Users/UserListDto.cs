namespace CivicHero.Backend.Core.DTOs.Users;

public sealed class UserListDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public long? WardId { get; set; }
    public string? WardName { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
