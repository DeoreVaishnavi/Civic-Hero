using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class StaffAccountEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StaffAccountEndpointTests(CivicHeroWebApplicationFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Theory]
    [InlineData("GET", "/api/v1/staff-accounts")]
    [InlineData("GET", "/api/v1/staff-accounts/metadata")]
    [InlineData("GET", "/api/v1/staff-accounts/pending")]
    [InlineData("POST", "/api/v1/staff-accounts")]
    [InlineData("POST", "/api/v1/staff-accounts/123/review")]
    public async Task Staff_account_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new
            {
                fullName = "Authorization Test Officer",
                email = "staff.authorization@example.com",
                phone = "+919876543210",
                role = "Officer",
                departmentId = 1,
                wardId = 1,
                temporaryPassword = "Secure@123",
                confirmPassword = "Secure@123",
                decision = "Approve",
                remarks = "Authorization endpoint test."
            });
        }

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
