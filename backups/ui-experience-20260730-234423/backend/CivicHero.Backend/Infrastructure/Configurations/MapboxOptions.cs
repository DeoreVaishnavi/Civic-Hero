namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class MapboxOptions
{
    public const string SectionName = "Mapbox";

    public string AccessToken { get; set; } = string.Empty;
    public string StyleUrl { get; set; } = "mapbox://styles/mapbox/streets-v12";
    public double DefaultLatitude { get; set; } = 19.0760;
    public double DefaultLongitude { get; set; } = 72.8777;
    public double DefaultZoom { get; set; } = 10.5;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !AccessToken.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) &&
        !AccessToken.Contains("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);
}
