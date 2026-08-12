using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

public sealed partial class CivicDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CivicDbContext).Assembly);

        modelBuilder.Entity<User>().HasQueryFilter(user => !user.IsDeleted);
        modelBuilder.Entity<Complaint>().HasQueryFilter(complaint => !complaint.IsDeleted);
    }
}
