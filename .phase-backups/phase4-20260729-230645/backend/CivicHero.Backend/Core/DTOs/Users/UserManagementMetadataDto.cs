namespace CivicHero.Backend.Core.DTOs.Users;

public sealed class UserManagementMetadataDto
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<LookupDto> Departments { get; init; } = Array.Empty<LookupDto>();
    public IReadOnlyList<WardLookupDto> Wards { get; init; } = Array.Empty<WardLookupDto>();
}

public class LookupDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public sealed class WardLookupDto : LookupDto
{
    public long DepartmentId { get; init; }
}
