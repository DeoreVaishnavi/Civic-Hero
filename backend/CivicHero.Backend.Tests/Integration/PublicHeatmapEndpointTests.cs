using System.Net;

namespace CivicHero.Backend.Tests.Integration;

public sealed class PublicHeatmapEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PublicHeatmapEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Public_heatmap_should_allow_anonymous_requests_and_reject_non_public_statuses()
    {
        using var response = await _client.GetAsync("/api/v1/analytics/public/heatmap?status=Created");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Internal_heatmap_should_still_require_authentication()
    {
        using var response = await _client.GetAsync("/api/v1/analytics/heatmap");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
