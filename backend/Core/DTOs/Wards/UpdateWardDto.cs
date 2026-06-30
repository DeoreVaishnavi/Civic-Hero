namespace CivicHero.Backend.Core.DTOs.Wards;

public class UpdateWardDto
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? BoundaryGeoJson { get; set; }
}