using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.DTOs.Rewards;
using CivicHero.Backend.Core.Enums;
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

public sealed class SaveRewardCatalogValidator : AbstractValidator<SaveRewardCatalogRequest>
{
    public SaveRewardCatalogValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(1000);
        RuleFor(request => request.PointsCost).InclusiveBetween(1, 1_000_000);
        RuleFor(request => request.StockQuantity).InclusiveBetween(0, 1_000_000);
        RuleFor(request => request.Type)
            .Must(value => Enum.TryParse<RewardType>(value, true, out _))
            .WithMessage("Reward type must be Voucher, Certificate, Badge, or Merchandise.");
    }
}

public sealed class AdjustRewardStockValidator : AbstractValidator<AdjustRewardStockRequest>
{
    public AdjustRewardStockValidator()
    {
        RuleFor(request => request.Quantity).InclusiveBetween(1, 1_000_000);
        RuleFor(request => request.Reason).NotEmpty().MinimumLength(10).MaximumLength(300);
    }
}

public sealed class ManualPointsAdjustmentValidator : AbstractValidator<ManualPointsAdjustmentRequest>
{
    public ManualPointsAdjustmentValidator()
    {
        RuleFor(request => request.UserId).GreaterThan(0);
        RuleFor(request => request.PointsDelta)
            .NotEqual(0)
            .InclusiveBetween(-10_000, 10_000);
        RuleFor(request => request.Reason).NotEmpty().MinimumLength(10).MaximumLength(250);
    }
}

public sealed class UpdateRedemptionStatusValidator : AbstractValidator<UpdateRedemptionStatusRequest>
{
    private static readonly RedemptionStatus[] AllowedStatuses =
    [
        RedemptionStatus.Approved,
        RedemptionStatus.Fulfilled,
        RedemptionStatus.Rejected,
        RedemptionStatus.Cancelled
    ];

    public UpdateRedemptionStatusValidator()
    {
        RuleFor(request => request.Status)
            .Must(value => Enum.TryParse<RedemptionStatus>(value, true, out var status) && AllowedStatuses.Contains(status))
            .WithMessage("Status must be Approved, Fulfilled, Rejected, or Cancelled.");
        RuleFor(request => request.Reason).NotEmpty().MinimumLength(10).MaximumLength(300);
    }
}

public sealed class SaveBadgeRulesValidator : AbstractValidator<SaveBadgeRulesRequest>
{
    private static readonly string[] SupportedMetrics =
    [
        "Points",
        "ClosedComplaints",
        "ApprovedVerifications",
        "SubmittedComplaints",
        "SupportedIssues"
    ];

    public SaveBadgeRulesValidator()
    {
        RuleFor(request => request.Rules).NotNull().Must(rules => rules is { Count: > 0 and <= 25 })
            .WithMessage("Provide between 1 and 25 badge rules.");
        RuleFor(request => request.Rules)
            .Must(rules => rules is not null && rules.Select(rule => rule.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == rules.Count)
            .WithMessage("Badge rule codes must be unique.");
        RuleForEach(request => request.Rules).ChildRules(rule =>
        {
            rule.RuleFor(item => item.Code).NotEmpty().Matches("^[A-Za-z0-9_]+$").MaximumLength(60);
            rule.RuleFor(item => item.Name).NotEmpty().MaximumLength(120);
            rule.RuleFor(item => item.Description).NotEmpty().MaximumLength(500);
            rule.RuleFor(item => item.Icon).NotEmpty().MaximumLength(20);
            rule.RuleFor(item => item.Metric).Must(metric => SupportedMetrics.Contains(metric, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Badge metric is not supported.");
            rule.RuleFor(item => item.Target).InclusiveBetween(1, 1_000_000);
            rule.RuleFor(item => item.DisplayOrder).InclusiveBetween(0, 1000);
        });
    }
}

public sealed class SaveTierRulesValidator : AbstractValidator<SaveTierRulesRequest>
{
    public SaveTierRulesValidator()
    {
        RuleFor(request => request.Rules).NotNull().Must(rules => rules is { Count: > 0 and <= 20 })
            .WithMessage("Provide between 1 and 20 tier rules.");
        RuleFor(request => request.Rules)
            .Must(rules => rules is not null && rules.Select(rule => rule.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == rules.Count)
            .WithMessage("Tier names must be unique.");
        RuleFor(request => request.Rules)
            .Must(rules => rules is not null && rules.Select(rule => rule.MinimumPoints).Distinct().Count() == rules.Count)
            .WithMessage("Tier minimum-point values must be unique.");
        RuleFor(request => request.Rules)
            .Must(rules => rules is not null && rules.Any(rule => rule.MinimumPoints == 0))
            .WithMessage("One tier must start at zero points.");
        RuleForEach(request => request.Rules).ChildRules(rule =>
        {
            rule.RuleFor(item => item.Name).NotEmpty().MaximumLength(100);
            rule.RuleFor(item => item.MinimumPoints).InclusiveBetween(0, 10_000_000);
        });
    }
}
