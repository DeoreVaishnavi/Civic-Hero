namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class ComplaintMetadataResponse
{
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DepartmentOptionDto> Departments { get; init; } = Array.Empty<DepartmentOptionDto>();
    public IReadOnlyList<WardOptionDto> Wards { get; init; } = Array.Empty<WardOptionDto>();
}

public sealed class DepartmentOptionDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public sealed class WardOptionDto
{
    public long Id { get; init; }
    public long DepartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}
