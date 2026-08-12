using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Enums;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class ChangeRoleValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleValidator()
    {
        RuleFor(request => request.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => Enum.TryParse<UserRole>(role, true, out _))
            .WithMessage("Role must be Citizen, Officer, Supervisor, Admin, or SuperAdmin.");
    }
}
