namespace CivicHero.Backend.Core.DTOs.Complaints;

public sealed class NearbyComplaintQuery
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public double RadiusKm { get; set; } = 5;
    public int Limit { get; set; } = 30;
}
