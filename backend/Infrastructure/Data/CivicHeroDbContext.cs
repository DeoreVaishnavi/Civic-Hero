using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data
{
    public class CivicHeroDbContext : DbContext
    {
        public CivicHeroDbContext(DbContextOptions<CivicHeroDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AiFraudAnalysis> AiFraudAnalyses { get; set; }
        public DbSet<RewardCatalog> RewardCatalogs { get; set; }
        public DbSet<Redemption> Redemptions { get; set; }
        public DbSet<ReputationLog> ReputationLogs { get; set; }
        public DbSet<DisputeAuditLog> DisputeAuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
public DbSet<Complaint> Complaints { get; set; }
public DbSet<ComplaintUpdate> ComplaintUpdates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CivicHeroDbContext).Assembly);
        }
    }
}