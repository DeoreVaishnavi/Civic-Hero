using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class AdminRewardEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminRewardEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("GET", "/api/v1/admin/rewards/catalog")]
    [InlineData("GET", "/api/v1/admin/rewards/rules")]
    [InlineData("GET", "/api/v1/admin/rewards/redemptions")]
    [InlineData("POST", "/api/v1/admin/rewards/catalog")]
    [InlineData("PUT", "/api/v1/admin/rewards/catalog/1")]
    [InlineData("POST", "/api/v1/admin/rewards/catalog/1/stock")]
    [InlineData("POST", "/api/v1/admin/rewards/catalog/1/activate")]
    [InlineData("DELETE", "/api/v1/admin/rewards/catalog/1")]
    [InlineData("PUT", "/api/v1/admin/rewards/rules/badges")]
    [InlineData("PUT", "/api/v1/admin/rewards/rules/tiers")]
    [InlineData("POST", "/api/v1/admin/rewards/points/adjust")]
    [InlineData("PUT", "/api/v1/admin/rewards/redemptions/1/status")]
    public async Task Admin_reward_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            request.Content = JsonContent.Create(new
            {
                name = "Authorization test reward",
                description = "This payload verifies that reward administration requires authentication.",
                pointsCost = 100,
                type = "Voucher",
                stockQuantity = 10,
                isActive = true,
                quantity = 5,
                userId = 1,
                pointsDelta = 10,
                reason = "Authorization test reason.",
                status = "Approved",
                rules = Array.Empty<object>()
            });
        }

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
