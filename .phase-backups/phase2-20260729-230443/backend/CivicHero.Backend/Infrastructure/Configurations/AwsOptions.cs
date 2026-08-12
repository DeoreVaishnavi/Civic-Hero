namespace CivicHero.Backend.Infrastructure.Configurations;

public sealed class AwsOptions
{
    public const string SectionName = "AWS";

    public string Region { get; set; } = "ap-south-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string BucketName { get; set; } = "civichero-storage";

    public bool HasExplicitCredentials =>
        IsConfiguredValue(AccessKey) && IsConfiguredValue(SecretKey);

    public bool HasBucketConfiguration => IsConfiguredValue(BucketName);

    private static bool IsConfiguredValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}
