namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class AwsOptions
{
    public const string SectionName = "AWS";

    public string Region { get; set; } = "ap-south-1";
    public string S3BucketName { get; set; } = string.Empty;
    public string S3ServiceUrl { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; }
    public int PresignedUrlExpiryMinutes { get; set; } = 15;
}
