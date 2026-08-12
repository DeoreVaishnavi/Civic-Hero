using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class WardSeed
{
    public static IReadOnlyList<Ward> Create(IReadOnlyDictionary<string, long> departmentIds) =>
    [
        new() { Name = "Central Ward", Code = "WARD-CENTRAL", DepartmentId = departmentIds["ROADS"] },
        new() { Name = "North Ward", Code = "WARD-NORTH", DepartmentId = departmentIds["WASTE"] },
        new() { Name = "South Ward", Code = "WARD-SOUTH", DepartmentId = departmentIds["LIGHT"] },
        new() { Name = "East Ward", Code = "WARD-EAST", DepartmentId = departmentIds["WATER"] },
        new() { Name = "West Ward", Code = "WARD-WEST", DepartmentId = departmentIds["DRAIN"] }
    ];
}
