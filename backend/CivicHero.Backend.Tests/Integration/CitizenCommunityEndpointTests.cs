using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class CitizenCommunityEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CitizenCommunityEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("POST", "/api/v1/complaints/123/follow")]
    [InlineData("DELETE", "/api/v1/complaints/123/follow")]
    [InlineData("GET", "/api/v1/complaints/123/follow-status")]
    [InlineData("GET", "/api/v1/complaints/following/ids")]
    [InlineData("GET", "/api/v1/complaints/following")]
    public async Task Complaint_following_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Edit_comment_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.PutAsJsonAsync("/api/v1/complaints/123/comments/456", new
        {
            body = "Updated public comment text."
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Report_comment_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/complaints/123/comments/456/report", new
        {
            category = "Unsafe",
            reason = "This comment reveals private information."
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
