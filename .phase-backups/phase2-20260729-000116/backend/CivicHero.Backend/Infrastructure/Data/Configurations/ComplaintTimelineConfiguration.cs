using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintTimelineConfiguration : IEntityTypeConfiguration<ComplaintTimeline>
{
    public void Configure(EntityTypeBuilder<ComplaintTimeline> builder)
    {
        builder.ToTable("complaint_timelines");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.EventType).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(entity => new { entity.ComplaintId, entity.Timestamp });
        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.Timeline)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.User)
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
