using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class DepartmentSeed
{
    public static IReadOnlyList<Department> Create() =>
    [
        new() { Name = "Roads and Potholes", Code = "ROADS", Description = "Road surfaces, potholes and footpaths" },
        new() { Name = "Solid Waste Management", Code = "WASTE", Description = "Garbage collection and illegal dumping" },
        new() { Name = "Street Lighting", Code = "LIGHT", Description = "Streetlights and public electrical faults" },
        new() { Name = "Water Supply", Code = "WATER", Description = "Water leakage and supply issues" },
        new() { Name = "Drainage", Code = "DRAIN", Description = "Drain blockage, sewage and flooding" }
    ];
}
