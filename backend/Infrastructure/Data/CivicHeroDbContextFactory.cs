using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CivicHero.Backend.Infrastructure.Data
{
    /// <summary>
    /// Design-time DbContext factory for EF Core Migrations.
    /// This allows EF Core tools to create the DbContext without relying on the application's DI container.
    /// </summary>
    public class CivicHeroDbContextFactory : IDesignTimeDbContextFactory<CivicHeroDbContext>
    {
        public CivicHeroDbContext CreateDbContext(string[] args)
        {
            // Build configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<CivicHeroDbContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to a default connection string for design-time
                connectionString = "Server=localhost;Port=3306;Database=civicherodb;Uid=root;Pwd=password;";
            }

            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new CivicHeroDbContext(optionsBuilder.Options);
        }
    }
}