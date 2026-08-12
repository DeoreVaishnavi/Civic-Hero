using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ReputationLogConfiguration : IEntityTypeConfiguration<ReputationLog>
{
    public void Configure(EntityTypeBuilder<ReputationLog> builder)
    {
        builder.ToTable("reputation_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(80);
        builder.HasOne(x => x.User).WithMany(x => x.ReputationLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
