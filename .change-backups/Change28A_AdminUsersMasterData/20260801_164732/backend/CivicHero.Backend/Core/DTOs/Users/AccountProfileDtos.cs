using Microsoft.AspNetCore.Http;

namespace CivicHero.Backend.Core.DTOs.Users;

public sealed class ChangeEmailRequest
{
    public string NewEmail { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
}

public sealed class ChangeEmailResponse
{
    public string Email { get; set; } = string.Empty;
    public bool RequiresVerification { get; set; } = true;
    public DateTimeOffset VerificationExpiresAtUtc { get; set; }
    public string? DevelopmentVerificationToken { get; set; }
}

public sealed class UploadProfileAvatarRequest
{
    public IFormFile? File { get; set; }
}

public sealed record ProfileAvatarContent(Stream Content, string ContentType, string FileName);

public sealed record ProfileAvatarResult(
    bool HasAvatar,
    string Action,
    DateTimeOffset CompletedAtUtc);
