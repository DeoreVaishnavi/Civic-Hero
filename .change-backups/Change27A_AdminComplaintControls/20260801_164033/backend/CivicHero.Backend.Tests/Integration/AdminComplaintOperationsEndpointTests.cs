using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class AdminComplaintOperationsEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;
    public AdminComplaintOperationsEndpointTests(CivicHeroWebApplicationFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Theory]
    [InlineData("GET", "/api/v1/admin/complaints")]
    [InlineData("GET", "/api/v1/admin/complaints/duplicate-clusters")]
    [InlineData("GET", "/api/v1/admin/complaints/123")]
    [InlineData("PUT", "/api/v1/admin/complaints/123/priority")]
    [InlineData("PUT", "/api/v1/admin/complaints/123/routing")]
    [InlineData("POST", "/api/v1/admin/complaints/123/assignment-override")]
    [InlineData("POST", "/api/v1/admin/complaints/123/close")]
    [InlineData("POST", "/api/v1/admin/complaints/123/reopen")]
    [InlineData("POST", "/api/v1/admin/complaints/123/archive")]
    [InlineData("POST", "/api/v1/admin/complaints/123/restore")]
    [InlineData("POST", "/api/v1/admin/complaints/123/link-duplicate")]
    [InlineData("POST", "/api/v1/admin/complaints/123/merge")]
    [InlineData("POST", "/api/v1/admin/complaints/123/media/1/remove")]
    public async Task Admin_complaint_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new
            {
                priority = "High",
                departmentId = 1,
                wardId = 1,
                officerId = 1,
                canonicalComplaintId = 2,
                reason = "Administrative authorization test reason."
            });
        }
        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
