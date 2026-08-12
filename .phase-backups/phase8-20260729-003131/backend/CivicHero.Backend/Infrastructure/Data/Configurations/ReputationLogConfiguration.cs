using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ReputationLogConfiguration : IEntityTypeConfiguration<ReputationLog>
{
    public void Configure(EntityTypeBuilder<ReputationLog> builder)
    {
        builder.ToTable("reputation_logs");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Reason).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.ReferenceType).HasMaxLength(80);
        builder.HasIndex(entity => new { entity.UserId, entity.CreatedAt });
        builder.HasOne(entity => entity.User)
            .WithMany(entity => entity.ReputationLogs)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
