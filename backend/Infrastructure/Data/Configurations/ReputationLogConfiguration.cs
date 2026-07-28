using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class ReputationLogConfiguration : IEntityTypeConfiguration<ReputationLog>
    {
        public void Configure(EntityTypeBuilder<ReputationLog> builder)
        {
            builder.ToTable("ReputationLogs");

            builder.HasKey(rl => rl.Id);

            builder.Property(rl => rl.PointsChange)
                .IsRequired();

            builder.Property(rl => rl.Reason)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(rl => rl.Timestamp)
                .IsRequired();

            builder.HasOne(rl => rl.User)
                .WithMany(u => u.ReputationLogs)
                .HasForeignKey(rl => rl.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
