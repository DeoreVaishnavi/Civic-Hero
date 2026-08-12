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
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<CivicDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("CivicHeroDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:CivicHeroDatabase is not configured. " +
                "Use dotnet user-secrets or the ConnectionStrings__CivicHeroDatabase environment variable.");
        }

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var optionsBuilder = new DbContextOptionsBuilder<CivicDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.Parse($"{databaseOptions.MySqlVersion}-mysql"),
            mySql =>
            {
                mySql.MigrationsAssembly(typeof(CivicDbContext).Assembly.FullName);
                mySql.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });

        return new CivicDbContext(optionsBuilder.Options);
    }
}
