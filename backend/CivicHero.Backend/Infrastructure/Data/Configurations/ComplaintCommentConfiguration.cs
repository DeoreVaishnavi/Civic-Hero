using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class ComplaintCommentConfiguration : IEntityTypeConfiguration<ComplaintComment>
{
    public void Configure(EntityTypeBuilder<ComplaintComment> builder)
    {
        builder.ToTable("complaint_comments");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Body).HasMaxLength(1500).IsRequired();
        builder.Property(entity => entity.Visibility).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.ModerationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.ModerationReason).HasMaxLength(500);
        builder.HasIndex(entity => new { entity.ComplaintId, entity.CreatedAt });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
        builder.HasOne(entity => entity.Complaint).WithMany(entity => entity.Comments)
            .HasForeignKey(entity => entity.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(entity => entity.User).WithMany(entity => entity.ComplaintComments)
            .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ModeratedByUser).WithMany()
            .HasForeignKey(entity => entity.ModeratedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
