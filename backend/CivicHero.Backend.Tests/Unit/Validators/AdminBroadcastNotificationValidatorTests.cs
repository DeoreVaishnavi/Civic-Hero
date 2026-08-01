using CivicHero.Backend.Core.DTOs.Notifications;
using CivicHero.Backend.Core.Validators;
using FluentValidation.TestHelper;

namespace CivicHero.Backend.Tests.Unit.Validators;

public sealed class AdminBroadcastNotificationValidatorTests
{
    private readonly AdminBroadcastNotificationValidator _validator = new();

    [Fact]
    public void All_active_users_should_pass_validation()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Legacy_role_payload_should_remain_compatible()
    {
        var request = ValidRequest();
        request.AudienceType = string.Empty;
        request.Role = "Citizen";

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Department_audience_should_require_department_id()
    {
        var request = ValidRequest();
        request.AudienceType = "Department";

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(item => item.DepartmentId);
    }

    [Fact]
    public void Ward_audience_should_reject_mixed_filters()
    {
        var request = ValidRequest();
        request.AudienceType = "Ward";
        request.WardId = 7;
        request.Role = "Officer";

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(item => item.AudienceType);
    }


    [Fact]
    public void Predefined_group_should_pass_validation()
    {
        var request = ValidRequest();
        request.AudienceType = "Group";
        request.GroupIdentifier = "FieldOperations";

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Selected_users_should_require_unique_positive_ids()
    {
        var request = ValidRequest();
        request.AudienceType = "SelectedUsers";
        request.SelectedUserIds = [12, 12, 0];

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(item => item.SelectedUserIds);
    }

    private static AdminBroadcastNotificationRequest ValidRequest() => new()
    {
        AudienceType = "All",
        Title = "Service update",
        Message = "A CivicHero service update is available.",
        Type = "General",
        SendInApp = true
    };
}
