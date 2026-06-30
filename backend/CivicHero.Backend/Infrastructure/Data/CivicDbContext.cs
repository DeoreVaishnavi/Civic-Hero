using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

/// <summary>
/// Represents the application's primary Entity Framework Core database context.
/// It is responsible for managing database connections, entity tracking,
/// and executing database operations.
/// </summary>
public sealed class CivicDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CivicDbContext"/> class.
    /// </summary>
    /// <param name="options">
    /// Database context configuration supplied by Dependency Injection.
    /// </param>
    public CivicDbContext(DbContextOptions<CivicDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the EF Core model.
    /// Entity configurations will be added as modules are implemented.
    /// </summary>
    /// <param name="modelBuilder">
    /// Model builder used to configure entity mappings.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically apply IEntityTypeConfiguration<T> classes
        // from this assembly when they are added in future steps.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CivicDbContext).Assembly);
    }
}