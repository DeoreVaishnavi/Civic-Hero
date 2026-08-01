using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Validation;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(request => request.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(PersonNameRules.MinimumLength)
                .WithMessage($"Full name must contain at least {PersonNameRules.MinimumLength} letters.")
            .MaximumLength(PersonNameRules.MaximumLength)
                .WithMessage($"Full name cannot exceed {PersonNameRules.MaximumLength} characters.")
            .Must(PersonNameRules.IsValid)
                .WithMessage("Full name can contain letters and spaces only. Digits and special characters are not allowed.");

        RuleFor(request => request.Phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
            .Matches(@"^[0-9+()\-\s]*$").WithMessage("Phone number contains unsupported characters.")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
    }
}
