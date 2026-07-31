using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintVoteConfiguration : IEntityTypeConfiguration<ComplaintVote>
{
    public void Configure(EntityTypeBuilder<ComplaintVote> builder)
    {
        builder.ToTable("complaint_votes");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => new { entity.ComplaintId, entity.UserId }).IsUnique();
        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.Votes)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.ComplaintVotes)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
