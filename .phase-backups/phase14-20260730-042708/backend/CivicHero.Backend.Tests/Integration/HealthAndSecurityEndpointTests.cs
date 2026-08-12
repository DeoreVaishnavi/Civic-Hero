using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class HealthAndSecurityEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthAndSecurityEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Live_health_endpoint_should_be_healthy_and_include_security_headers()
    {
        using var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Content-Type-Options", out var values).Should().BeTrue();
        values!.Should().Contain("nosniff");
        response.Headers.TryGetValues("X-Correlation-ID", out _).Should().BeTrue();

        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Protected_security_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.GetAsync("/api/v1/security/session");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_endpoint_should_return_not_found()
    {
        using var response = await _client.GetAsync("/api/v1/does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record HealthPayload(string Status, double TotalDurationMs);
}
