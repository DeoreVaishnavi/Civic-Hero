using CivicHero.Backend.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

/// <summary>
/// Primary Entity Framework Core database context
/// for the CivicHero application.
/// </summary>
public sealed partial class CivicDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CivicDbContext"/> class.
    /// </summary>
    public CivicDbContext(
        DbContextOptions<CivicDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }
}