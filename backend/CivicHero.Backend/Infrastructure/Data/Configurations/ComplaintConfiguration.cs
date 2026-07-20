using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Complaint.
/// </summary>
public sealed class ComplaintConfiguration
    : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");

        //---------------------------------------------------
        // Primary Key
        //---------------------------------------------------

        builder.HasKey(x => x.Id);

        //---------------------------------------------------
        // Properties
        //---------------------------------------------------

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        //---------------------------------------------------
        // GeoLocation Value Object
        //---------------------------------------------------

        builder.OwnsOne(x => x.Location, location =>
        {
            location.Property(l => l.Latitude)
                .HasColumnName("Latitude")
                .HasPrecision(10, 7)
                .IsRequired();

            location.Property(l => l.Longitude)
                .HasColumnName("Longitude")
                .HasPrecision(10, 7)
                .IsRequired();
        });

        //---------------------------------------------------
        // Relationships
        //---------------------------------------------------

        builder.HasOne(x => x.Citizen)
            .WithMany()
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Ward)
            .WithMany()
            .HasForeignKey(x => x.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Contractor)
            .WithMany()
            .HasForeignKey(x => x.ContractorId)
            .OnDelete(DeleteBehavior.SetNull);

        //---------------------------------------------------
        // Child Collections
        //---------------------------------------------------

        builder.Navigation(x => x.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.TimelineEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.Votes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.FraudAnalyses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        //---------------------------------------------------
        // Indexes
        //---------------------------------------------------

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.Priority);

        builder.HasIndex(x => x.CitizenId);

        builder.HasIndex(x => x.DepartmentId);

        builder.HasIndex(x => x.ContractorId);

        builder.HasIndex(x => x.WardId);

        builder.HasIndex(x => new
        {
            x.Status,
            x.Priority
        });

        builder.HasIndex(x => new
        {
            x.DepartmentId,
            x.Status
        });
    }
}