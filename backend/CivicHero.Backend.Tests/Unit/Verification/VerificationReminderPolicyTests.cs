using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Tests.Unit.Verification;

public sealed class VerificationReminderPolicyTests
{
    [Fact]
    public void Assigned_officer_should_receive_two_reminders_with_a_twelve_hour_cooldown()
    {
        var now = DateTimeOffset.Parse("2026-08-02T00:00:00+00:00");

        var allowed = VerificationReminderPolicy.Evaluate("Officer", 0, null, now);
        var coolingDown = VerificationReminderPolicy.Evaluate("Officer", 1, now.AddHours(-2), now);
        var exhausted = VerificationReminderPolicy.Evaluate("Officer", 2, now.AddHours(-24), now);

        allowed.Allowed.Should().BeTrue();
        allowed.MaximumAllowed.Should().Be(2);
        coolingDown.Allowed.Should().BeFalse();
        coolingDown.NextAllowedAt.Should().Be(now.AddHours(10));
        exhausted.Allowed.Should().BeFalse();
        exhausted.UnavailableReason.Should().Contain("limit");
    }

    [Fact]
    public void Supervisor_should_use_the_staff_limit_and_six_hour_cooldown()
    {
        var now = DateTimeOffset.Parse("2026-08-02T00:00:00+00:00");

        var decision = VerificationReminderPolicy.Evaluate("Supervisor", 3, now.AddHours(-7), now);

        decision.Allowed.Should().BeTrue();
        decision.MaximumAllowed.Should().Be(4);
    }

    [Fact]
    public void Citizen_should_never_be_allowed_to_send_a_verification_reminder()
    {
        var decision = VerificationReminderPolicy.Evaluate("Citizen", 0, null, DateTimeOffset.UtcNow);

        decision.Allowed.Should().BeFalse();
        decision.MaximumAllowed.Should().Be(0);
    }
}
