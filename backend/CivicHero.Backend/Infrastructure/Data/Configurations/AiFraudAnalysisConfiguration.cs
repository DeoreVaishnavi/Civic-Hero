using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AiFraudAnalysis.
/// </summary>
public sealed class AiFraudAnalysisConfiguration
    : IEntityTypeConfiguration<AiFraudAnalysis>
{
    public void Configure(EntityTypeBuilder<AiFraudAnalysis> builder)
    {
        //-----------------------------------------------------
        // Table
        //-----------------------------------------------------

      builder.ToTable("AiFraudAnalyses", table =>
{
    table.HasCheckConstraint(
        "CK_AiFraudAnalysis_ConfidenceScore",
        "`ConfidenceScore` >= 0 AND `ConfidenceScore` <= 1");
});

        //-----------------------------------------------------
        // Primary Key
        //-----------------------------------------------------

        builder.HasKey(x => x.Id);

        //-----------------------------------------------------
        // Properties
        //-----------------------------------------------------

        builder.Property(x => x.ModelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ModelVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ConfidenceScore)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(x => x.IsFraudDetected)
            .IsRequired();

        builder.Property(x => x.AnalysisSummary)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.AnalyzedOnUtc)
            .IsRequired();

        //-----------------------------------------------------
        // Relationship
        //-----------------------------------------------------

        builder.HasOne(x => x.Complaint)
            .WithMany(x => x.FraudAnalyses)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        //-----------------------------------------------------
        // Performance Indexes
        //-----------------------------------------------------

        builder.HasIndex(x => x.ComplaintId);

        builder.HasIndex(x => x.IsFraudDetected);

        builder.HasIndex(x => x.ConfidenceScore);

        builder.HasIndex(x => x.AnalyzedOnUtc);

        builder.HasIndex(x => new
        {
            x.ComplaintId,
            x.AnalyzedOnUtc
        });

        builder.HasIndex(x => new
        {
            x.IsFraudDetected,
            x.ConfidenceScore
        });
    }
}