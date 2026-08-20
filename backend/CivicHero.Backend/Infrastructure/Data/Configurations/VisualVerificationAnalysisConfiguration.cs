using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class VisualVerificationAnalysisConfiguration : IEntityTypeConfiguration<VisualVerificationAnalysis>
{
    public void Configure(EntityTypeBuilder<VisualVerificationAnalysis> builder)
    {
        builder.ToTable("visual_verification_analyses");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Provider).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Verdict).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.CompletionScore).HasPrecision(5, 4);
        builder.Property(entity => entity.ImageQualityScore).HasPrecision(5, 4);
        builder.Property(entity => entity.ManipulationRiskScore).HasPrecision(5, 4);
        builder.Property(entity => entity.OverallConfidence).HasPrecision(5, 4);
        builder.Property(entity => entity.Reasoning).HasColumnType("text").IsRequired();
        builder.Property(entity => entity.ObservationsJson).HasColumnType("json").IsRequired();
        builder.Property(entity => entity.BeforeImageIdsJson).HasColumnType("json").IsRequired();
        builder.Property(entity => entity.AfterImageIdsJson).HasColumnType("json").IsRequired();
        builder.Property(entity => entity.EvidenceFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RawResponse).HasColumnType("longtext");
        builder.Property(entity => entity.HumanDecision).HasMaxLength(40);
        builder.Property(entity => entity.HumanNotes).HasMaxLength(1500);
        builder.HasIndex(entity => new { entity.ComplaintId, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.ComplaintId, entity.EvidenceFingerprint }).IsUnique();
        builder.HasIndex(entity => new { entity.RequiresHumanReview, entity.ReviewedAt });
        builder.HasOne(entity => entity.Complaint).WithMany(entity => entity.VisualVerificationAnalyses)
            .HasForeignKey(entity => entity.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.ReviewedByUser).WithMany()
            .HasForeignKey(entity => entity.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
