using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Validation;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
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

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(request => request.Phone)
            .Matches(@"^[0-9+()\-\s]{7,20}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone))
            .WithMessage("Phone number format is invalid.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");

        RuleFor(request => request.ConfirmPassword)
            .Equal(request => request.Password)
            .WithMessage("Password and confirmation password must match.");
    }
}
