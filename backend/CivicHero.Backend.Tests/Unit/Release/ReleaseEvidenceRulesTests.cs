using FluentAssertions;

namespace CivicHero.Backend.Tests.Unit.Release;

public sealed class ReleaseEvidenceRulesTests
{
    [Fact]
    public void Release_is_blocked_when_a_required_gate_is_skipped()
    {
        var gates = new[] { (Required: true, Status: "Passed"), (Required: true, Status: "Skipped") };
        var approved = gates.Where(item => item.Required).All(item => item.Status == "Passed");
        approved.Should().BeFalse();
    }

    [Fact]
    public void Release_is_approved_only_when_every_required_gate_passes()
    {
        var gates = new[] { (Required: true, Status: "Passed"), (Required: true, Status: "Passed"), (Required: false, Status: "Skipped") };
        var approved = gates.Where(item => item.Required).All(item => item.Status == "Passed");
        approved.Should().BeTrue();
    }
}
