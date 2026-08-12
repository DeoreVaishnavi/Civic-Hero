using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text").IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Citizen).WithMany(x => x.ComplaintsCreated).HasForeignKey(x => x.CitizenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedOfficer).WithMany(x => x.AssignedComplaints).HasForeignKey(x => x.AssignedOfficerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Department).WithMany(x => x.Complaints).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Ward).WithMany(x => x.Complaints).HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CitizenId);
        builder.HasIndex(x => new { x.DepartmentId, x.WardId, x.Status });
        builder.HasIndex(x => new { x.Category, x.CreatedAt });
        builder.HasIndex(x => new { x.Latitude, x.Longitude });
    }
}
