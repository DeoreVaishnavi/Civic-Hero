using CivicHero.Backend.Core.DTOs.Complaints;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class UpdateComplaintValidator : AbstractValidator<UpdateComplaintRequest>
{
    public UpdateComplaintValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(5).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(20).MaximumLength(5000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.WardId).GreaterThan(0);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Address).NotEmpty().MinimumLength(5).MaximumLength(500);
    }
}
