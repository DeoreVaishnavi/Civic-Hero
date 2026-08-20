using CivicHero.Backend.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace CivicHero.Backend.Tests.Unit.Verification;

public sealed class VerificationReminderAuthorizationContractTests
{
    [Fact]
    public void Reminder_endpoint_should_allow_only_officer_and_supervisory_roles()
    {
        var method = typeof(VerificationController).GetMethod(nameof(VerificationController.Remind));
        method.Should().NotBeNull();

        var roles = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SelectMany(attribute => (attribute.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        roles.Should().BeEquivalentTo("Officer", "Supervisor", "Admin", "SuperAdmin");
        roles.Should().NotContain("Citizen");
    }
}
