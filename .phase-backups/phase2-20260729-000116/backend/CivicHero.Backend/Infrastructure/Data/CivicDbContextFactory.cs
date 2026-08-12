using CivicHero.Backend.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CivicHero.Backend.Infrastructure.Data;

public sealed class CivicDbContextFactory : IDesignTimeDbContextFactory<CivicDbContext>
{
    public CivicDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<CivicDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "ConnectionStrings:DefaultConnection is missing.");

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var optionsBuilder = new DbContextOptionsBuilder<CivicDbContext>();
        ConfigureMySql(optionsBuilder, connectionString, databaseOptions);

        return new CivicDbContext(optionsBuilder.Options);
    }

    public static void ConfigureMySql(
        DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        DatabaseOptions databaseOptions)
    {
        var version = ParseServerVersion(databaseOptions.ServerVersion);

        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(version),
            mysqlOptions =>
            {
                mysqlOptions.CommandTimeout(Math.Max(1, databaseOptions.CommandTimeout));

                if (databaseOptions.EnableRetryOnFailure)
                {
                    mysqlOptions.EnableRetryOnFailure(
                        Math.Max(1, databaseOptions.MaxRetryCount),
                        TimeSpan.FromSeconds(Math.Max(1, databaseOptions.MaxRetryDelaySeconds)),
                        errorNumbersToAdd: null);
                }
            });

        optionsBuilder.EnableDetailedErrors(databaseOptions.EnableDetailedErrors);
        optionsBuilder.EnableSensitiveDataLogging(databaseOptions.EnableSensitiveDataLogging);
    }

    private static Version ParseServerVersion(string? configuredVersion)
    {
        return Version.TryParse(configuredVersion, out var version)
            ? version
            : new Version(8, 0, 40);
    }
}
