using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class AdminNotificationEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminNotificationEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("GET", "/api/v1/admin/notifications/templates")]
    [InlineData("POST", "/api/v1/admin/notifications/templates")]
    [InlineData("PUT", "/api/v1/admin/notifications/templates/test-template")]
    [InlineData("DELETE", "/api/v1/admin/notifications/templates/test-template")]
    [InlineData("POST", "/api/v1/admin/notifications/broadcast")]
    [InlineData("GET", "/api/v1/admin/notifications/deliveries")]
    [InlineData("GET", "/api/v1/admin/notifications/deliveries/summary")]
    [InlineData("POST", "/api/v1/admin/notifications/deliveries/1/retry")]
    [InlineData("GET", "/api/v1/admin/notifications/schedules")]
    [InlineData("DELETE", "/api/v1/admin/notifications/schedules/0123456789abcdef0123456789abcdef")]
    public async Task Admin_notification_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            request.Content = JsonContent.Create(new
            {
                key = "test-template",
                name = "Test template",
                titleTemplate = "Test title",
                messageTemplate = "Test notification message.",
                title = "Test title",
                message = "Test notification message.",
                type = "General",
                sendInApp = true,
                reason = "Authorization test reason."
            });
        }

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
