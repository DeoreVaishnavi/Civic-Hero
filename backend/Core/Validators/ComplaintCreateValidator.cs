using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public class ComplaintCreateValidator :
    AbstractValidator<ComplaintCreateRequest>
{
    public ComplaintCreateValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(3000);

        RuleFor(x => x.CitizenId)
            .GreaterThan(0);

        RuleFor(x => x.WardId)
            .GreaterThan(0);

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(x => x.Address)
            .NotEmpty()
            .MaximumLength(500);
    }
}