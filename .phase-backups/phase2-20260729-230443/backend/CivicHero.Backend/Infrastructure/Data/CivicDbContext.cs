using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

public sealed partial class CivicDbContext : DbContext
{
    public CivicDbContext(DbContextOptions<CivicDbContext> options)
        : base(options)
    {
    }
}
