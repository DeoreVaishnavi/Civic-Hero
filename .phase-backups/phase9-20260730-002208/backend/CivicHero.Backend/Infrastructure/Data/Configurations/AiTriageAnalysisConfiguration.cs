using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class AiTriageAnalysisConfiguration : IEntityTypeConfiguration<AiTriageAnalysis>
{
    public void Configure(EntityTypeBuilder<AiTriageAnalysis> builder)
    {
        builder.ToTable("ai_triage_analyses");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Provider).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.PredictedCategory).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ClassificationConfidence).HasPrecision(5, 4);
        builder.Property(entity => entity.DuplicateStatus).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.DuplicateScore).HasPrecision(5, 4);
        builder.Property(entity => entity.FraudVerdict).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.FraudScore).HasPrecision(5, 4);
        builder.Property(entity => entity.PredictedPriority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.PriorityScore).HasPrecision(5, 4);
        builder.Property(entity => entity.Reasoning).HasColumnType("text");
        builder.Property(entity => entity.RawProviderResponse).HasColumnType("longtext");
        builder.Property(entity => entity.ReviewDecision).HasMaxLength(40);
        builder.Property(entity => entity.ReviewNotes).HasColumnType("text");
        builder.HasIndex(entity => new { entity.ComplaintId, entity.AnalyzedAt });
        builder.HasIndex(entity => new { entity.RequiresManualReview, entity.ReviewedAt });
        builder.HasOne(entity => entity.Complaint)
            .WithMany(entity => entity.TriageAnalyses)
            .HasForeignKey(entity => entity.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
