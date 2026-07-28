using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations
{
    public class AiFraudAnalysisConfiguration : IEntityTypeConfiguration<AiFraudAnalysis>
    {
        public void Configure(EntityTypeBuilder<AiFraudAnalysis> builder)
        {
            builder.ToTable("ai_fraud_analysis");
            builder.HasKey(x => x.id);
            builder.Property(x => x.FraudScore)
            .IsRequired();

            builder.Property(x => x.RiskLevel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.Reasons)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.Reviewed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.ComplaintId);

            builder.HasIndex(x => x.FraudScore);

            builder.HasIndex(x => x.RiskLevel);

            //builder.HasOne(x => x.Complaint)
            //.WithMany()
            //.HasForeignKey(x => x.ComplaintId)
            //.OnDelete(DeleteBehavior.Cascade);
        }
    }
}
