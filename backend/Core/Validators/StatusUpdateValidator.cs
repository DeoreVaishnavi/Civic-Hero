using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public class StatusUpdateValidator :
    AbstractValidator<StatusUpdateRequest>
{
    public StatusUpdateValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}