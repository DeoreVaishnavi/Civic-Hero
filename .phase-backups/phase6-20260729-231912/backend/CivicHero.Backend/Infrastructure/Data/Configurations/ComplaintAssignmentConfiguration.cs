using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintAssignmentConfiguration : IEntityTypeConfiguration<ComplaintAssignment>
{
    public void Configure(EntityTypeBuilder<ComplaintAssignment> builder)
    {
        builder.ToTable("complaint_assignments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.ComplaintId, entity.IsCurrent });
        builder.HasIndex(entity => new { entity.OfficerId, entity.Status, entity.IsCurrent });
        builder.HasIndex(entity => entity.AssignmentDueAt);
        builder.HasIndex(entity => entity.ResolutionDueAt);

        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.Assignments)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Officer)
            .WithMany(entity => entity.OfficerAssignments)
            .HasForeignKey(entity => entity.OfficerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.AssignedBy)
            .WithMany(entity => entity.AssignmentsCreated)
            .HasForeignKey(entity => entity.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
