using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CivicHero.Backend.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.UserEmail).HasMaxLength(256);
        builder.Property(entity => entity.UserRole).HasMaxLength(40);
        builder.Property(entity => entity.Action).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.EntityId).HasMaxLength(100);
        builder.Property(entity => entity.OldValuesJson).HasColumnType("longtext");
        builder.Property(entity => entity.NewValuesJson).HasColumnType("longtext");
        builder.Property(entity => entity.IpAddress).HasMaxLength(64);
        builder.Property(entity => entity.UserAgent).HasMaxLength(500);
        builder.Property(entity => entity.CorrelationId).HasMaxLength(100);
        builder.Property(entity => entity.Severity).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(entity => entity.CreatedAt);
        builder.HasIndex(entity => new { entity.UserId, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.EntityName, entity.EntityId, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.Success, entity.CreatedAt });
    }
}
