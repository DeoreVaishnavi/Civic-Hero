namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class MapboxOptions
{
    public const string SectionName = "Mapbox";

    public string AccessToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;
}
