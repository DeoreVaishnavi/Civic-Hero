using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class UpsertComplaintDraftValidator : AbstractValidator<UpsertComplaintDraftRequest>
{
    private static readonly HashSet<string> AllowedSeverity = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Critical"
    };

    public UpsertComplaintDraftValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Category).MaximumLength(80);
        RuleFor(x => x.CitizenSeverity)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSeverity.Contains(value.Trim()))
            .WithMessage("Select Low, Medium, High, or Critical as the reported severity.");
        RuleFor(x => x.DepartmentId).GreaterThan(0).When(x => x.DepartmentId.HasValue);
        RuleFor(x => x.WardId).GreaterThan(0).When(x => x.WardId.HasValue);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Address).MaximumLength(400);
        RuleFor(x => x.Landmark).MaximumLength(100);
        RuleFor(x => x.EmergencyReason).MaximumLength(1000);
    }
}

public sealed class AddComplaintDraftEvidenceValidator : AbstractValidator<AddComplaintDraftEvidenceRequest>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
        "video/mp4", "video/webm", "video/quicktime",
        "application/pdf", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public AddComplaintDraftEvidenceValidator()
    {
        RuleFor(x => x.Evidence)
            .NotNull()
            .Must(file => file is not null && file.Length > 0 && file.Length <= 15L * 1024 * 1024)
            .WithMessage("Each draft evidence file must be between 1 byte and 15 MB.")
            .Must(file => file is not null && AllowedContentTypes.Contains(file.ContentType ?? string.Empty))
            .WithMessage("Draft evidence must be JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC, or DOCX.");
    }
}
