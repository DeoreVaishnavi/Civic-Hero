namespace CivicHero.Backend.Core.Constants;

/// <summary>
/// Central location for application-wide constants.
/// Avoid using hard-coded values throughout the project.
/// </summary>
public static class AppConstants
{
    public static class Validation
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;

        public const int EmailMaxLength = 255;

        public const int PhoneNumberMaxLength = 20;

        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 100;

        public const int ComplaintTitleMaxLength = 200;
        public const int ComplaintDescriptionMaxLength = 5000;
    }

    public static class Pagination
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }

    public static class Security
    {
        public const string JwtScheme = "Bearer";

        public const string AdminPolicy = "Administrator";

        public const string OfficerPolicy = "Officer";

        public const string CitizenPolicy = "Citizen";
    }

    public static class Cache
    {
        public const int DefaultExpirationMinutes = 30;
    }

    public static class FileUpload
    {
        public const int MaxImageSizeInMb = 10;

        public static readonly string[] AllowedImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };
    }

    public static class Rewards
    {
        public const int InitialPoints = 0;
    }
}