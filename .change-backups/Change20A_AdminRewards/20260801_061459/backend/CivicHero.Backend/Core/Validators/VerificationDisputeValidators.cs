using CivicHero.Backend.Core.DTOs.Verification;
using CivicHero.Backend.Core.DTOs.Disputes;
using FluentValidation;
namespace CivicHero.Backend.Core.Validators;
public sealed class VerifyComplaintValidator : AbstractValidator<VerifyComplaintRequest>
{
    public VerifyComplaintValidator() { RuleFor(x => x.Rating).InclusiveBetween(1,5); RuleFor(x => x.Remarks).MaximumLength(1500); RuleFor(x => x.Latitude).InclusiveBetween(-90,90); RuleFor(x => x.Longitude).InclusiveBetween(-180,180); }
}
public sealed class RaiseDisputeValidator : AbstractValidator<RaiseDisputeRequest>
{ public RaiseDisputeValidator() { RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1500); } }
public sealed class DisputeDecisionValidator : AbstractValidator<DisputeDecisionRequest>
{ public DisputeDecisionValidator() { RuleFor(x => x.Decision).Must(v => new[]{"Rework","Close","Uphold","Fraud"}.Contains(v, StringComparer.OrdinalIgnoreCase)); RuleFor(x => x.Remarks).NotEmpty().MaximumLength(1500); } }
public sealed class AppealDisputeValidator : AbstractValidator<AppealDisputeRequest>
{ public AppealDisputeValidator() { RuleFor(x => x.Remarks).NotEmpty().MinimumLength(10).MaximumLength(1500); } }
