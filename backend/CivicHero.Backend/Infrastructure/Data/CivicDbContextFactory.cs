using CivicHero.Backend.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CivicHero.Backend.Infrastructure.Data;

/// <summary>
/// Design-time factory used by Entity Framework Core
/// to create <see cref="CivicDbContext"/> instances for
/// migrations and database updates.
/// </summary>
public sealed class CivicDbContextFactory
    : IDesignTimeDbContextFactory<CivicDbContext>
{
    /// <summary>
    /// Creates a new <see cref="CivicDbContext"/> instance.
    /// </summary>
    public CivicDbContext CreateDbContext(string[] args)
    {
        // Resolve the project directory regardless of where the command is run.
        string basePath = Directory.GetCurrentDirectory();

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .Build();

        string connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

    DbContextOptions<CivicDbContext> options =
    new DbContextOptionsBuilder<CivicDbContext>()
        .UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString))
        .Options;

        return new CivicDbContext(
            options,
            new DesignTimeCurrentUserService());
    }

    /// <summary>
    /// Design-time implementation of ICurrentUserService.
    /// Used only by EF Core migration tools.
    /// </summary>
    private sealed class DesignTimeCurrentUserService
        : ICurrentUserService
    {
        public Guid? UserId => null;
    }
}