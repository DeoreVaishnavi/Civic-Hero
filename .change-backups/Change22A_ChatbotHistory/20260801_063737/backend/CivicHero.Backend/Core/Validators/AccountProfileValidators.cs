using CivicHero.Backend.Core.DTOs.Users;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class ChangeEmailValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailValidator()
    {
        RuleFor(request => request.NewEmail)
            .NotEmpty().WithMessage("New email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        RuleFor(request => request.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.")
            .MaximumLength(128);
    }
}
