using CivicHero.Backend.Core.DTOs.Assignments;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class AssignComplaintValidator : AbstractValidator<AssignComplaintRequest>
{
    public AssignComplaintValidator()
    {
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.OfficerId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class ReassignComplaintValidator : AbstractValidator<ReassignComplaintRequest>
{
    public ReassignComplaintValidator()
    {
        RuleFor(x => x.OfficerId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RejectAssignmentValidator : AbstractValidator<RejectAssignmentRequest>
{
    public RejectAssignmentValidator() => RuleFor(x => x.Reason).NotEmpty().MinimumLength(5).MaximumLength(500);
}

public sealed class AddProgressValidator : AbstractValidator<AddProgressRequest>
{
    public AddProgressValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.ProgressPercent).InclusiveBetween(1, 99);
        RuleFor(x => x.EstimatedCompletionAt)
            .Must(value => !value.HasValue || value.Value > DateTimeOffset.UtcNow.AddMinutes(10))
            .WithMessage("Estimated completion must be at least ten minutes in the future.");
    }
}

public sealed class AddProgressEvidenceValidator : AbstractValidator<AddProgressEvidenceRequest>
{
    public AddProgressEvidenceValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.ProgressPercent).InclusiveBetween(1, 99);
        RuleFor(x => x.Evidence).NotEmpty().Must(files => files.Count <= 5)
            .WithMessage("Upload between one and five progress evidence files.");
        RuleFor(x => x.EstimatedCompletionAt)
            .Must(value => !value.HasValue || value.Value > DateTimeOffset.UtcNow.AddMinutes(10))
            .WithMessage("Estimated completion must be at least ten minutes in the future.");
    }
}

public sealed class TransferAssignmentRequestValidator : AbstractValidator<TransferAssignmentRequest>
{
    private static readonly string[] AllowedReasons =
    ["WrongDepartment", "OutsideScope", "WorkloadCapacity", "SafetyConcern", "SpecialistRequired", "Other"];

    public TransferAssignmentRequestValidator()
    {
        RuleFor(x => x.ReasonCode).NotEmpty().Must(value => AllowedReasons.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Select a valid transfer reason.");
        RuleFor(x => x.Details).NotEmpty().MinimumLength(10).MaximumLength(1000);
    }
}

public sealed class OfficerCitizenInformationRequestValidator : AbstractValidator<OfficerCitizenInformationRequest>
{
    public OfficerCitizenInformationRequestValidator() =>
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(1500);
}

public sealed class CompleteAssignmentValidator : AbstractValidator<CompleteAssignmentRequest>
{
    public CompleteAssignmentValidator()
    {
        RuleFor(x => x.Notes).NotEmpty().MinimumLength(10).MaximumLength(1000);
        RuleFor(x => x.Evidence).NotEmpty().Must(files => files.Count <= 5).WithMessage("Upload no more than five completion evidence files.");
    }
}

public sealed class BulkAssignValidator : AbstractValidator<BulkAssignRequest>
{
    public BulkAssignValidator()
    {
        RuleFor(x => x.OfficerId).GreaterThan(0);
        RuleFor(x => x.ComplaintIds).NotEmpty().Must(ids => ids.Count <= 50).WithMessage("A maximum of 50 complaints can be assigned at once.");
        RuleForEach(x => x.ComplaintIds).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class SupervisorActionRequestValidator : AbstractValidator<SupervisorActionRequest>
{
    public SupervisorActionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(10).MaximumLength(1500);
    }
}

public sealed class SupervisorMessageRequestValidator : AbstractValidator<SupervisorMessageRequest>
{
    public SupervisorMessageRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MinimumLength(10).MaximumLength(1500);
    }
}
