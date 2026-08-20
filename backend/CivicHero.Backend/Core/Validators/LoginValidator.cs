using CivicHero.Backend.Core.DTOs.Auth;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(request => request.EffectiveIdentifier)
            .NotEmpty().WithMessage("Email address or phone number is required.")
            .MaximumLength(256);
        RuleFor(request => request.Password).NotEmpty();
        RuleFor(request => request.TwoFactorCode).MaximumLength(40)
            .When(request => !string.IsNullOrWhiteSpace(request.TwoFactorCode));
    }
}
