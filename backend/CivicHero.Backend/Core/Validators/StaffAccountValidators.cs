using CivicHero.Backend.Core.DTOs.Staff;
using CivicHero.Backend.Core.Validation;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class CreateStaffAccountValidator : AbstractValidator<CreateStaffAccountRequest>
{
    public CreateStaffAccountValidator()
    {
        RuleFor(request => request.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Full name is required.")
            .Must(PersonNameRules.IsValid)
            .WithMessage("Full name can contain letters and spaces only.");

        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        RuleFor(request => request.Phone)
            .Matches(@"^[0-9+()\-\s]{7,20}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone))
            .WithMessage("Phone number format is invalid.");

        RuleFor(request => request.Role)
            .Must(role => string.Equals(role, "Officer", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(role, "Supervisor", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only Officer or Supervisor accounts can be created here.");

        RuleFor(request => request.DepartmentId)
            .GreaterThan(0).WithMessage("Department is required.");

        RuleFor(request => request.WardId)
            .NotNull().WithMessage("Ward is required for an Officer account.")
            .When(request => string.Equals(request.Role, "Officer", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.TemporaryPassword)
            .NotEmpty().WithMessage("Temporary password is required.")
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");

        RuleFor(request => request.ConfirmPassword)
            .Equal(request => request.TemporaryPassword)
            .WithMessage("Password and confirmation password must match.");
    }
}

public sealed class ReviewStaffAccountValidator : AbstractValidator<ReviewStaffAccountRequest>
{
    public ReviewStaffAccountValidator()
    {
        RuleFor(request => request.Decision)
            .Must(value => string.Equals(value, "Approve", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(value, "Reject", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Decision must be Approve or Reject.");

        RuleFor(request => request.Remarks)
            .MaximumLength(500);
    }
}
