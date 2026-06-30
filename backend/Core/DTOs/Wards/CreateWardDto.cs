namespace CivicHero.Backend.Core.DTOs.Wards
{
    public class CreateWardDto
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? BoundaryGeoJson { get; set; }
    }
}
