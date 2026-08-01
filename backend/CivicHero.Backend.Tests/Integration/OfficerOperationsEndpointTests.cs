using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class OfficerOperationsEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OfficerOperationsEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("POST", "/api/v1/assignments/123/transfer-request")]
    [InlineData("POST", "/api/v1/assignments/123/request-citizen-information")]
    [InlineData("GET", "/api/v1/assignments/dashboard/officer/performance?days=90")]
    [InlineData("GET", "/api/v1/assignments/map/officer")]
    public async Task Officer_management_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            request.Content = JsonContent.Create(new
            {
                reasonCode = "OutsideScope",
                details = "This assignment requires a different field team.",
                message = "Please provide a clearer landmark and suitable access time."
            });
        }

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Progress_evidence_endpoint_should_reject_anonymous_request()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Field inspection completed."), "Message");
        content.Add(new StringContent("40"), "ProgressPercent");
        content.Add(new ByteArrayContent([0xFF, 0xD8, 0xFF]), "Evidence", "progress.jpg");

        using var response = await _client.PostAsync("/api/v1/assignments/123/progress-evidence", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
