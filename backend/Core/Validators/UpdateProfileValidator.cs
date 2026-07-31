using CivicHero.Backend.Core.DTOs.Users;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(request => request.Phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
            .Matches(@"^[0-9+()\-\s]*$").WithMessage("Phone number contains unsupported characters.")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone));
    }
}
