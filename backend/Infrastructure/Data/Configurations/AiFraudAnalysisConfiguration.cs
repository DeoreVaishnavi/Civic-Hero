using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class AiFraudAnalysisConfiguration : IEntityTypeConfiguration<AiFraudAnalysis>
{
    public void Configure(EntityTypeBuilder<AiFraudAnalysis> builder)
    {
        builder.ToTable("ai_fraud_analyses");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Verdict).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Reasoning).HasColumnType("text");
        builder.Property(entity => entity.Provider).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(120).IsRequired();
        builder.HasIndex(entity => new { entity.ComplaintId, entity.AnalyzedAt });
        builder.HasIndex(entity => entity.RequiresManualReview);
        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.FraudAnalyses)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
