using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Data.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Extensions;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDefaultConnectionAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<DatabaseOptions>>()
            .Value;

        if (!options.ApplyMigrationsOnStartup && !options.SeedDataOnStartup)
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<CivicDbContext>();

        if (options.ApplyMigrationsOnStartup)
        {
            await dbContext.Database.MigrateAsync();
        }

        if (options.SeedDataOnStartup)
        {
            await DatabaseSeeder.SeedAsync(dbContext);
        }
    }
}

