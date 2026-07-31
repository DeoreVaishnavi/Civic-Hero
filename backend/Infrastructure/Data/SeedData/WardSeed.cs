using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class WardSeed
{
    private static readonly DateTimeOffset SeedDate = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyCollection<Ward> GetWards() =>
    [
        Create(1, 1, "Ward A", "WARD-A", 19.1300m, 18.8900m, 72.9900m, 72.7700m),
        Create(2, 2, "Ward B", "WARD-B", 19.1500m, 18.9200m, 73.0100m, 72.7900m),
        Create(3, 3, "Ward C", "WARD-C", 19.1700m, 18.9400m, 73.0300m, 72.8100m),
        Create(4, 4, "Ward D", "WARD-D", 19.1900m, 18.9600m, 73.0500m, 72.8300m),
        Create(5, 5, "Ward E", "WARD-E", 19.2100m, 18.9800m, 73.0700m, 72.8500m),
        Create(6, 6, "Ward F", "WARD-F", 19.2300m, 19.0000m, 73.0900m, 72.8700m)
    ];

    private static Ward Create(
        long id,
        long departmentId,
        string name,
        string code,
        decimal north,
        decimal south,
        decimal east,
        decimal west) => new()
    {
        Id = id,
        DepartmentId = departmentId,
        Name = name,
        Code = code,
        BoundaryNorth = north,
        BoundarySouth = south,
        BoundaryEast = east,
        BoundaryWest = west,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = SeedDate,
        UpdatedAt = SeedDate
    };
}
