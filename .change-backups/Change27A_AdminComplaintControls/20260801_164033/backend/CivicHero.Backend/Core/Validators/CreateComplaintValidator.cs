using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class CreateComplaintValidator : AbstractValidator<CreateComplaintRequest>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
        "video/mp4", "video/webm", "video/quicktime",
        "application/pdf", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private static readonly HashSet<string> AllowedSeverity = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Critical"
    };

    public CreateComplaintValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(5000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.CitizenSeverity).Must(value => AllowedSeverity.Contains(value ?? string.Empty))
            .WithMessage("Select Low, Medium, High, or Critical as the reported severity.");
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.WardId).GreaterThan(0);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Address).NotEmpty().MinimumLength(5).MaximumLength(400);
        RuleFor(x => x.Landmark).MaximumLength(100);
        RuleFor(x => x.EmergencyReason).NotEmpty().MinimumLength(10).MaximumLength(1000)
            .When(x => x.PossibleEmergency)
            .WithMessage("Explain why this may be an emergency.");

        RuleFor(x => x).Must(request => Combined(request).Count <= 8)
            .WithMessage("A maximum of eight evidence files can be uploaded.");
        RuleFor(x => x).Must(request => Combined(request).Sum(file => file.Length) <= 25L * 1024 * 1024)
            .WithMessage("The total evidence upload must not exceed 25 MB.");
        RuleFor(x => x).Must(request => Combined(request).All(file =>
            file.Length > 0 && file.Length <= 15L * 1024 * 1024 && AllowedContentTypes.Contains(file.ContentType ?? string.Empty)))
            .WithMessage("Evidence must be a supported JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC, or DOCX file, no larger than 15 MB each.");
    }

    private static IReadOnlyList<Microsoft.AspNetCore.Http.IFormFile> Combined(CreateComplaintRequest request) =>
        request.Evidence.Concat(request.Images).ToArray();
}
