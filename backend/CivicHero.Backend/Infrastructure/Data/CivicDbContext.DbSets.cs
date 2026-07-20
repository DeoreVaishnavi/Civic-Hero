using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Data;

/// <summary>
/// Contains all DbSet properties used by the application.
/// Each DbSet represents a database table.
/// </summary>
public sealed partial class CivicDbContext
{
    #region User Management

    /// <summary>
    /// Application users.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Contractors.
    /// </summary>
    public DbSet<Contractor> Contractors => Set<Contractor>();

    #endregion

    #region Organization

    /// <summary>
    /// Departments.
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// Wards.
    /// </summary>
    public DbSet<Ward> Wards => Set<Ward>();

    #endregion

    #region Complaints

    /// <summary>
    /// Citizen complaints.
    /// </summary>
    public DbSet<Complaint> Complaints => Set<Complaint>();

    /// <summary>
    /// Complaint images.
    /// </summary>
    public DbSet<ComplaintImage> ComplaintImages => Set<ComplaintImage>();

    /// <summary>
    /// Complaint timeline entries.
    /// </summary>
    public DbSet<ComplaintTimeline> ComplaintTimelines => Set<ComplaintTimeline>();

    /// <summary>
    /// Complaint votes.
    /// </summary>
    public DbSet<ComplaintVote> ComplaintVotes => Set<ComplaintVote>();

    /// <summary>
    /// AI fraud analysis records.
    /// </summary>
    public DbSet<AiFraudAnalysis> AiFraudAnalyses => Set<AiFraudAnalysis>();

    /// <summary>
    /// Dispute audit logs.
    /// </summary>
    public DbSet<DisputeAuditLog> DisputeAuditLogs => Set<DisputeAuditLog>();

    #endregion

    #region Notifications

    /// <summary>
    /// User notifications.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    #endregion

    #region Chat

    /// <summary>
    /// Chat sessions.
    /// </summary>
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();

    /// <summary>
    /// Chat messages.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    #endregion

    #region Rewards

    /// <summary>
    /// Reward catalog.
    /// </summary>
    public DbSet<RewardCatalog> RewardCatalogs => Set<RewardCatalog>();

    /// <summary>
    /// Reputation history.
    /// </summary>
    public DbSet<ReputationLog> ReputationLogs => Set<ReputationLog>();

    /// <summary>
    /// Reward redemptions.
    /// </summary>
    public DbSet<Redemption> Redemptions => Set<Redemption>();

    #endregion
}