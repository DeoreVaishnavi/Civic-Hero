using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CivicHero.Backend.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for registering infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all infrastructure services required by the application.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ------------------------------------------------------------
        // Strongly Typed Options
        // ------------------------------------------------------------

        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<AiOptions>(
            configuration.GetSection(AiOptions.SectionName));

        services.Configure<MapboxOptions>(
            configuration.GetSection(MapboxOptions.SectionName));

       // ------------------------------------------------------------
// Database
// ------------------------------------------------------------

var databaseOptions = configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>();

ArgumentNullException.ThrowIfNull(databaseOptions);

var connectionString = configuration.GetConnectionString("DefaultConnection");

ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

services.AddDbContext<CivicDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.CommandTimeout(databaseOptions.CommandTimeout);
        });

    options.EnableSensitiveDataLogging(
        databaseOptions.EnableSensitiveDataLogging);

    options.EnableDetailedErrors(
        databaseOptions.EnableDetailedErrors);
});
        // ------------------------------------------------------------
        // Health Checks
        // ------------------------------------------------------------

        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<AiHealthCheck>("ai");

        return services;
    }
}