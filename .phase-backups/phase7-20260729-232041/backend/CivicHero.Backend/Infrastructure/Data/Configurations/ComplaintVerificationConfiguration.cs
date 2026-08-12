using CivicHero.Backend.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CivicHero.Backend.Infrastructure.Data.Configurations;
public sealed class ComplaintVerificationConfiguration : IEntityTypeConfiguration<ComplaintVerification>
{
 public void Configure(EntityTypeBuilder<ComplaintVerification> b){b.ToTable("complaint_verifications");b.HasKey(x=>x.Id);b.HasIndex(x=>x.ComplaintId).IsUnique();b.HasIndex(x=>new{x.Decision,x.DueAt});b.Property(x=>x.Decision).HasConversion<string>().HasMaxLength(30);b.Property(x=>x.Remarks).HasMaxLength(1500);b.Property(x=>x.SubmittedLatitude).HasPrecision(10,7);b.Property(x=>x.SubmittedLongitude).HasPrecision(10,7);b.HasOne(x=>x.Complaint).WithOne(x=>x.Verification).HasForeignKey<ComplaintVerification>(x=>x.ComplaintId).OnDelete(DeleteBehavior.Cascade);b.HasOne(x => x.Citizen).WithMany(x => x.ComplaintVerifications).HasForeignKey(x => x.CitizenId).OnDelete(DeleteBehavior.Restrict);}
}
