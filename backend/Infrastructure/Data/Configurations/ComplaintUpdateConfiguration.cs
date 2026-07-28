using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class ComplaintUpdateConfiguration : IEntityTypeConfiguration<ComplaintUpdate>
    {
        public void Configure(EntityTypeBuilder<ComplaintUpdate> builder)
        {
            builder.ToTable("ComplaintUpdates");

            builder.HasKey(cu => cu.Id);

            builder.Property(cu => cu.UpdateType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(cu => cu.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(cu => cu.CreatedAt)
                .IsRequired();

            // Foreign keys
            builder.HasOne(cu => cu.Complaint)
                .WithMany(c => c.Updates)
                .HasForeignKey(cu => cu.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cu => cu.User)
                .WithMany() // We don't have a collection of ComplaintUpdate in User, but we can add if needed
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}