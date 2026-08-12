using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data.SeedData;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        CivicDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Departments.AnyAsync(cancellationToken))
        {
            await dbContext.Departments.AddRangeAsync(DepartmentSeed.Create(), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Wards.AnyAsync(cancellationToken))
        {
            var departmentIds = await dbContext.Departments
                .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);

            await dbContext.Wards.AddRangeAsync(WardSeed.Create(departmentIds), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
