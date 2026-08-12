using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class CreateComplaintValidator : AbstractValidator<CreateComplaintRequest>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public CreateComplaintValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(5000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.WardId).GreaterThan(0);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Address).NotEmpty().MinimumLength(5).MaximumLength(500);
        RuleFor(x => x.Images).Must(images => images.Count <= 5)
            .WithMessage("A maximum of five images can be uploaded.");
        RuleForEach(x => x.Images).ChildRules(file =>
        {
            file.RuleFor(x => x.Length).GreaterThan(0).LessThanOrEqualTo(5 * 1024 * 1024)
                .WithMessage("Each image must be between 1 byte and 5 MB.");
            file.RuleFor(x => x.ContentType).Must(type => AllowedContentTypes.Contains(type))
                .WithMessage("Only JPEG, PNG and WebP images are allowed.");
        });
    }
}
