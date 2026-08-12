using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

public sealed partial class CivicDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintImage> ComplaintImages => Set<ComplaintImage>();
    public DbSet<ComplaintTimeline> ComplaintTimelines => Set<ComplaintTimeline>();
    public DbSet<ComplaintVote> ComplaintVotes => Set<ComplaintVote>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RewardCatalog> RewardCatalogs => Set<RewardCatalog>();
    public DbSet<Redemption> Redemptions => Set<Redemption>();
    public DbSet<ReputationLog> ReputationLogs => Set<ReputationLog>();
    public DbSet<AiFraudAnalysis> AiFraudAnalyses => Set<AiFraudAnalysis>();
    public DbSet<DisputeAuditLog> DisputeAuditLogs => Set<DisputeAuditLog>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
}
