using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintVoteConfiguration : IEntityTypeConfiguration<ComplaintVote>
{
    public void Configure(EntityTypeBuilder<ComplaintVote> builder)
    {
        builder.ToTable("complaint_votes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ComplaintId, x.UserId }).IsUnique();
        builder.HasOne(x => x.Complaint).WithMany(x => x.Votes).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
