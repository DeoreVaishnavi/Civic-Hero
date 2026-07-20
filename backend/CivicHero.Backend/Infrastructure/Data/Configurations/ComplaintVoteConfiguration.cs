using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ComplaintVote.
/// </summary>
public sealed class ComplaintVoteConfiguration
    : IEntityTypeConfiguration<ComplaintVote>
{
    public void Configure(EntityTypeBuilder<ComplaintVote> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("ComplaintVotes");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Relationships
        //-----------------------------------------------------

        builder.HasOne(x => x.Complaint)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        //-----------------------------------------------------
        // Business Rule
        // One vote per user per complaint
        //-----------------------------------------------------

        builder.HasIndex(x => new
        {
            x.ComplaintId,
            x.UserId
        })
        .IsUnique();

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.CreatedOnUtc);
    }
}