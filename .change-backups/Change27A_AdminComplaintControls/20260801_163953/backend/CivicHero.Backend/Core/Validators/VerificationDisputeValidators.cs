using CivicHero.Backend.Core.DTOs.Verification;
using CivicHero.Backend.Core.DTOs.Disputes;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class VerifyComplaintValidator : AbstractValidator<VerifyComplaintRequest>
{
    private static readonly string[] ValidDecisions =
        ["Approved", "NotResolvedYet", "RequestRevisit", "PartiallyResolved", "Rejected"];

    public VerifyComplaintValidator()
    {
        RuleFor(x => x)
            .Must(HasValidDecision)
            .WithMessage("Decision must be Approved, NotResolvedYet, RequestRevisit, or PartiallyResolved.");

        RuleFor(x => x.Rating)
            .Must(value => value is >= 1 and <= 5)
            .When(IsApproved)
            .WithMessage("A rating between 1 and 5 is required when approving the resolution.");

        RuleFor(x => x.Rating)
            .Must(value => value is >= 1 and <= 5)
            .When(x => x.Rating.HasValue && !IsApproved(x))
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Remarks)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(1500)
            .When(x => !IsApproved(x))
            .WithMessage("Explain the unresolved issue in at least 10 characters.");

        RuleFor(x => x.Remarks).MaximumLength(1500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }

    private static bool HasValidDecision(VerifyComplaintRequest request)
    {
        if (request.Approved.HasValue && string.IsNullOrWhiteSpace(request.Decision)) return true;
        return ValidDecisions.Contains(request.Decision?.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsApproved(VerifyComplaintRequest request) =>
        request.Approved == true || string.Equals(request.Decision, "Approved", StringComparison.OrdinalIgnoreCase);
}

public sealed class SupervisorVerificationDecisionValidator : AbstractValidator<SupervisorVerificationDecisionRequest>
{
    public SupervisorVerificationDecisionValidator()
    {
        RuleFor(x => x.Remarks).NotEmpty().MinimumLength(10).MaximumLength(1500);
    }
}

public sealed class RaiseDisputeValidator : AbstractValidator<RaiseDisputeRequest>
{
    public RaiseDisputeValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1500);
    }
}

public sealed class DisputeDecisionValidator : AbstractValidator<DisputeDecisionRequest>
{
    public DisputeDecisionValidator()
    {
        RuleFor(x => x.Decision).Must(v => new[] { "Rework", "Close", "Uphold", "Fraud", "CitizenCorrect", "OfficerEvidenceSufficient", "Reopen", "AdditionalInvestigation", "EscalateToSuperAdmin", "OrderRework", "CloseDispute", "UpholdResolution" }.Contains(v, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(1500);
    }
}

public sealed class AppealDisputeValidator : AbstractValidator<AppealDisputeRequest>
{
    public AppealDisputeValidator()
    {
        RuleFor(x => x.Remarks).NotEmpty().MinimumLength(10).MaximumLength(1500);
    }
}
