using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
    {
        public void Configure(EntityTypeBuilder<Complaint> builder)
        {
            builder.ToTable("Complaints");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(c => c.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Address)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Latitude)
                .IsRequired();

            builder.Property(c => c.Longitude)
                .IsRequired();

            builder.Property(c => c.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.Priority)
                .IsRequired();

            builder.Property(c => c.IsAnonymous)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt);

            builder.Property(c => c.ResolvedAt);

            // Foreign keys
            builder.HasOne(c => c.User)
                .WithMany(u => u.Complaints) // We'll need to add this to User entity
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Ward)
                .WithMany(w => w.Complaints) // We added this to Ward
                .HasForeignKey(c => c.WardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Department)
                .WithMany(d => d.Complaints) // We added this to Department
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}