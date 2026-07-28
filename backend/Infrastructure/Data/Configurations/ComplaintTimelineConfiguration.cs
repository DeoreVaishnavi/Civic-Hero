using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public class ComplaintTimelineConfiguration :
    IEntityTypeConfiguration<ComplaintTimeline>
{
    public void Configure(
        EntityTypeBuilder<ComplaintTimeline> builder)
    {
        builder.ToTable("complaint_timeline");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.CreatedAt);
    }
}