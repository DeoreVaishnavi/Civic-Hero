using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class RoleSeed
{
    public static IReadOnlyCollection<UserRole> Roles { get; } =
        Enum.GetValues<UserRole>();
}
