using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class DepartmentSeed
{
    private static readonly DateTimeOffset SeedDate = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyCollection<Department> GetDepartments() =>
    [
        Create(1, "Roads & Potholes", "ROADS", "Road surface, pothole and footpath complaints."),
        Create(2, "Solid Waste Management", "WASTE", "Garbage collection and illegal dumping complaints."),
        Create(3, "Street Lighting", "LIGHT", "Streetlight and electrical public-infrastructure complaints."),
        Create(4, "Water Supply", "WATER", "Water leakage, low pressure and supply complaints."),
        Create(5, "Drainage & Sewerage", "DRAIN", "Blocked drains, sewage and flooding complaints."),
        Create(6, "Parks & Public Spaces", "PARKS", "Public garden and civic-space maintenance complaints.")
    ];

    private static Department Create(long id, string name, string code, string description) => new()
    {
        Id = id,
        Name = name,
        Code = code,
        Description = description,
        IsActive = true,
        IsDeleted = false,
        CreatedAt = SeedDate,
        UpdatedAt = SeedDate
    };
}
