using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintDraftEvidenceConfiguration : IEntityTypeConfiguration<ComplaintDraftEvidence>
{
    public void Configure(EntityTypeBuilder<ComplaintDraftEvidence> builder)
    {
        builder.ToTable("complaint_draft_evidence");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.S3Key).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(100).IsRequired();
        builder.HasIndex(entity => entity.ComplaintDraftId);
        builder.HasIndex(entity => entity.S3Key).IsUnique();
        builder.HasOne(entity => entity.ComplaintDraft)
            .WithMany(entity => entity.Evidence)
            .HasForeignKey(entity => entity.ComplaintDraftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
