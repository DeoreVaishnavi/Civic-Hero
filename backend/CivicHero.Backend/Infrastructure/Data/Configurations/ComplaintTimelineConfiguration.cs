using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ComplaintTimeline.
/// </summary>
public sealed class ComplaintTimelineConfiguration
    : IEntityTypeConfiguration<ComplaintTimeline>
{
    public void Configure(EntityTypeBuilder<ComplaintTimeline> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

        builder.ToTable("ComplaintTimelines");

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        //-----------------------------------------------------
        // Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.Complaint)
            .WithMany(x => x.TimelineEntries)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        //-----------------------------------------------------
        // Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.CreatedOnUtc);

        builder.HasIndex(x => new
        {
            x.ComplaintId,
            x.CreatedOnUtc
        });
    }
}