using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

public class CivicHeroDbContext : DbContext
{
    public CivicHeroDbContext(
        DbContextOptions<CivicHeroDbContext> options)
        : base(options)
    {
    }

    public DbSet<Complaint> Complaints => Set<Complaint>();

    public DbSet<ComplaintTimeline> ComplaintTimelines =>
        Set<ComplaintTimeline>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CivicHeroDbContext).Assembly);
    }
}