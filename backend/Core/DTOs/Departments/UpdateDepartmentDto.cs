namespace CivicHero.Backend.Core.DTOs.Departments;

public class UpdateDepartmentDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}