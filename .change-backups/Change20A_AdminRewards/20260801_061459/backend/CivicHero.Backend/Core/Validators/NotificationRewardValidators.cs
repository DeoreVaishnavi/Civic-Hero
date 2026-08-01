using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.DTOs.Rewards;
using FluentValidation;

namespace CivicHero.Backend.Core.Validators;

public sealed class BroadcastNotificationValidator : AbstractValidator<BroadcastNotificationRequest>
{
    public BroadcastNotificationValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Message).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.Role).Must(role => string.IsNullOrWhiteSpace(role) || new[] { "Citizen", "Officer", "Supervisor", "Admin", "SuperAdmin" }.Contains(role))
            .WithMessage("Role must be Citizen, Officer, Supervisor, Admin, or SuperAdmin.");
        RuleFor(request => request.ActionUrl).MaximumLength(500);
    }
}

public sealed class RedeemRewardValidator : AbstractValidator<RedeemRewardRequest>
{
    public RedeemRewardValidator() => RuleFor(request => request.RewardCatalogId).GreaterThan(0);
}
