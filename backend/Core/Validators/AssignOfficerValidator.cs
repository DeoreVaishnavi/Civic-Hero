using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public class AssignOfficerValidator :
    AbstractValidator<AssignOfficerRequest>
{
    public AssignOfficerValidator()
    {
        RuleFor(x => x.OfficerId)
            .GreaterThan(0);
    }
}