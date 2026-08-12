using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintTimelineConfiguration : IEntityTypeConfiguration<ComplaintTimeline>
{
    public void Configure(EntityTypeBuilder<ComplaintTimeline> builder)
    {
        builder.ToTable("complaint_timelines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.Complaint).WithMany(x => x.Timeline).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.ComplaintId, x.Timestamp });
    }
}
