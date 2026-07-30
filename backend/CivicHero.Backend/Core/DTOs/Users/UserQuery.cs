namespace CivicHero.Backend.Core.DTOs.Users;

public sealed class UserQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public long? DepartmentId { get; set; }
    public long? WardId { get; set; }
}
