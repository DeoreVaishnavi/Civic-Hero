using System.Net;

namespace CivicHero.Backend.Tests.Integration;

public sealed class ReportingEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportingEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("/api/v1/analytics/export?report=complaints&format=csv")]
    [InlineData("/api/v1/analytics/export?report=complaints&format=xlsx")]
    [InlineData("/api/v1/analytics/export?report=complaints&format=pdf")]
    [InlineData("/api/v1/analytics/export?report=citizen-engagement&format=csv")]
    [InlineData("/api/v1/admin/notifications/deliveries/export?format=xlsx")]
    [InlineData("/api/v1/admin/audit-logs/export?format=pdf")]
    public async Task Administrative_report_exports_should_reject_anonymous_requests(string endpoint)
    {
        using var response = await _client.GetAsync(endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
