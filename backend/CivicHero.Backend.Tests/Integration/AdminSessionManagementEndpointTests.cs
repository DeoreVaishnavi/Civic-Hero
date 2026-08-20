using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class AdminSessionManagementEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminSessionManagementEndpointTests(CivicHeroWebApplicationFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/api/v1/security/admin/sessions")]
    [InlineData("/api/v1/security/admin/sessions?search=citizen&role=Citizen&take=25")]
    public async Task Active_session_listing_should_reject_anonymous_requests(string endpoint)
    {
        using var response = await _client.GetAsync(endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Selected_user_session_revocation_should_reject_anonymous_requests()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/security/admin/sessions/test-session")
        {
            Content = JsonContent.Create(new
            {
                reason = "Authorization test for selected-session revocation."
            })
        };

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
