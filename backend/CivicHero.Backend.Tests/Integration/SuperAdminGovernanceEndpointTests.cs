using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class SuperAdminGovernanceEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;
    public SuperAdminGovernanceEndpointTests(CivicHeroWebApplicationFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Theory]
    [InlineData("GET", "/api/v1/superadmin/governance/admin-accounts")]
    [InlineData("POST", "/api/v1/superadmin/governance/admin-accounts")]
    [InlineData("PUT", "/api/v1/superadmin/governance/admin-accounts/10/active")]
    [InlineData("POST", "/api/v1/superadmin/governance/admin-accounts/10/reset-password")]
    [InlineData("GET", "/api/v1/superadmin/governance/policies")]
    [InlineData("PUT", "/api/v1/superadmin/governance/policies/roles")]
    [InlineData("PUT", "/api/v1/superadmin/governance/policies/authentication")]
    [InlineData("GET", "/api/v1/superadmin/governance/sessions")]
    [InlineData("DELETE", "/api/v1/superadmin/governance/sessions/test-session")]
    [InlineData("GET", "/api/v1/superadmin/governance/release-decisions")]
    [InlineData("POST", "/api/v1/superadmin/governance/release-decisions/Launch")]
    public async Task Endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (method is "POST" or "PUT" or "DELETE")
        {
            request.Content = JsonContent.Create(new
            {
                fullName = "Governance Test Admin",
                email = "governance.test@example.com",
                temporaryPassword = "Secure@123",
                markEmailVerified = true,
                isActive = false,
                decision = "Reject",
                reason = "Authorization test governance operation.",
                roles = Array.Empty<object>(),
                maximumFailedLoginAttempts = 5,
                lockoutMinutes = 30,
                requireVerifiedEmail = true,
                twoFactorRequiredRoles = Array.Empty<string>(),
                twoFactorEnrollmentGraceHours = 24
            });
        }
        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
