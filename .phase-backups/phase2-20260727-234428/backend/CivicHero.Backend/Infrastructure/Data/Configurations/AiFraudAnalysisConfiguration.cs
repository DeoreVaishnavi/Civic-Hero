using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class AiFraudAnalysisConfiguration : IEntityTypeConfiguration<AiFraudAnalysis>
{
    public void Configure(EntityTypeBuilder<AiFraudAnalysis> builder)
    {
        builder.ToTable("ai_fraud_analyses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Verdict).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Reasoning).HasColumnType("text");
        builder.HasOne(x => x.Complaint).WithMany(x => x.FraudAnalyses).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
    }
}
