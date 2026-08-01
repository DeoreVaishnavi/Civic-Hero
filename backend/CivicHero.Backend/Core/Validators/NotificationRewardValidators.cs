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

public sealed class SaveNotificationTemplateValidator : AbstractValidator<SaveNotificationTemplateRequest>
{
    public SaveNotificationTemplateValidator()
    {
        RuleFor(request => request.Key)
            .NotEmpty()
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]{1,59}$")
            .WithMessage("Template key must contain 2-60 letters, numbers, hyphens, or underscores.");
        RuleFor(request => request.Name).NotEmpty().MaximumLength(120);
        RuleFor(request => request.TitleTemplate).NotEmpty().MaximumLength(200);
        RuleFor(request => request.MessageTemplate).NotEmpty().MaximumLength(2000);
        RuleFor(request => request.Type)
            .Must(value => Enum.TryParse<NotificationType>(value, true, out _))
            .WithMessage("Notification type is not supported.");
        RuleFor(request => request.ActionUrlTemplate).MaximumLength(500);
    }
}

public sealed class AdminBroadcastNotificationValidator : AbstractValidator<AdminBroadcastNotificationRequest>
{
    private static readonly string[] Roles = ["Citizen", "Officer", "Supervisor", "Admin", "SuperAdmin"];
    private static readonly string[] AudienceTypes = ["All", "Role", "Department", "Ward", "Group", "SelectedUsers"];
    private static readonly string[] GroupIdentifiers = ["AllStaff", "FieldOperations", "Administrators", "CitizensWithActiveComplaints"];

    public AdminBroadcastNotificationValidator()
    {
        RuleFor(request => request)
            .Must(request => !string.IsNullOrWhiteSpace(request.TemplateKey) ||
                             (!string.IsNullOrWhiteSpace(request.Title) && !string.IsNullOrWhiteSpace(request.Message)))
            .WithMessage("Select a template or provide both title and message.");
        RuleFor(request => request.TemplateKey)
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]{1,59}$")
            .When(request => !string.IsNullOrWhiteSpace(request.TemplateKey));
        RuleFor(request => request.Title).MaximumLength(200);
        RuleFor(request => request.Message).MaximumLength(2000);
        RuleFor(request => request.AudienceType)
            .Must(value => string.IsNullOrWhiteSpace(value) || AudienceTypes.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Audience type must be All, Role, Department, Ward, Group, or SelectedUsers.");
        RuleFor(request => request.Role)
            .Must(role => string.IsNullOrWhiteSpace(role) || Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be Citizen, Officer, Supervisor, Admin, or SuperAdmin.");
        RuleFor(request => request.GroupIdentifier)
            .Must(group => string.IsNullOrWhiteSpace(group) || GroupIdentifiers.Contains(group, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Group must be AllStaff, FieldOperations, Administrators, or CitizensWithActiveComplaints.");
        RuleFor(request => request.SelectedUserIds)
            .Must(ids => ids is not null && ids.Count <= 200)
            .WithMessage("A broadcast can target at most 200 selected users.");
        RuleForEach(request => request.SelectedUserIds)
            .GreaterThan(0);
        RuleFor(request => request)
            .Custom(ValidateAudience);
        RuleFor(request => request.ActionUrl).MaximumLength(500);
        RuleFor(request => request.Type)
            .Must(value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<NotificationType>(value, true, out _))
            .WithMessage("Notification type is not supported.");
        RuleFor(request => request)
            .Must(request => request.SendInApp || request.SendSignalR || request.SendSms || request.SendEmail)
            .WithMessage("Select at least one delivery channel.");
        RuleFor(request => request.ScheduledFor)
            .Must(value => !value.HasValue || value.Value <= DateTimeOffset.UtcNow.AddYears(1))
            .WithMessage("A broadcast cannot be scheduled more than one year in advance.");
    }

    private static void ValidateAudience(AdminBroadcastNotificationRequest request, ValidationContext<AdminBroadcastNotificationRequest> context)
    {
        var type = string.IsNullOrWhiteSpace(request.AudienceType)
            ? (string.IsNullOrWhiteSpace(request.Role) ? "All" : "Role")
            : request.AudienceType.Trim();
        var selectedIds = request.SelectedUserIds ?? [];

        switch (type.ToLowerInvariant())
        {
            case "all":
                if (!string.IsNullOrWhiteSpace(request.Role) || request.DepartmentId.HasValue || request.WardId.HasValue || !string.IsNullOrWhiteSpace(request.GroupIdentifier) || selectedIds.Count > 0)
                    context.AddFailure("AudienceType", "All-user broadcasts cannot include role, department, ward, group, or selected-user filters.");
                break;
            case "role":
                if (string.IsNullOrWhiteSpace(request.Role))
                    context.AddFailure("Role", "Select a role for a role-targeted broadcast.");
                if (request.DepartmentId.HasValue || request.WardId.HasValue || !string.IsNullOrWhiteSpace(request.GroupIdentifier) || selectedIds.Count > 0)
                    context.AddFailure("AudienceType", "Role broadcasts cannot include department, ward, group, or selected-user filters.");
                break;
            case "department":
                if (!request.DepartmentId.HasValue)
                    context.AddFailure("DepartmentId", "Select a department group.");
                if (!string.IsNullOrWhiteSpace(request.Role) || request.WardId.HasValue || !string.IsNullOrWhiteSpace(request.GroupIdentifier) || selectedIds.Count > 0)
                    context.AddFailure("AudienceType", "Department broadcasts cannot include role, ward, group, or selected-user filters.");
                break;
            case "ward":
                if (!request.WardId.HasValue)
                    context.AddFailure("WardId", "Select a ward.");
                if (!string.IsNullOrWhiteSpace(request.Role) || request.DepartmentId.HasValue || !string.IsNullOrWhiteSpace(request.GroupIdentifier) || selectedIds.Count > 0)
                    context.AddFailure("AudienceType", "Ward broadcasts cannot include role, department, group, or selected-user filters.");
                break;
            case "group":
                if (string.IsNullOrWhiteSpace(request.GroupIdentifier))
                    context.AddFailure("GroupIdentifier", "Select a predefined notification group.");
                if (!string.IsNullOrWhiteSpace(request.Role) || request.DepartmentId.HasValue || request.WardId.HasValue || selectedIds.Count > 0)
                    context.AddFailure("AudienceType", "Group broadcasts cannot include role, department, ward, or selected-user filters.");
                break;
            case "selectedusers":
                if (selectedIds.Count == 0)
                    context.AddFailure("SelectedUserIds", "Select at least one recipient.");
                if (selectedIds.Count != selectedIds.Distinct().Count())
                    context.AddFailure("SelectedUserIds", "Selected-user IDs must be unique.");
                if (!string.IsNullOrWhiteSpace(request.Role) || request.DepartmentId.HasValue || request.WardId.HasValue || !string.IsNullOrWhiteSpace(request.GroupIdentifier))
                    context.AddFailure("AudienceType", "Selected-user broadcasts cannot include role, department, ward, or group filters.");
                break;
        }
    }
}

public sealed class RetryNotificationDeliveryValidator : AbstractValidator<RetryNotificationDeliveryRequest>
{
    public RetryNotificationDeliveryValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MinimumLength(10).MaximumLength(300);
    }
}
