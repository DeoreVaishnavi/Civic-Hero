using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CivicHero.Backend.Tests.Integration;

public sealed class DisputeOperationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DisputeOperationsEndpointTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/v1/disputes/officer")]
    [InlineData("/api/v1/disputes/1")]
    [InlineData("/api/v1/disputes/1/history")]
    [InlineData("/api/v1/disputes/1/evidence/1")]
    public async Task Protected_dispute_reads_require_authentication(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_final_decision_requires_authentication()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/disputes/1/superadmin-decision",
            new { decision = "Close", remarks = "Final evidence review completed." });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reopen_request_requires_authentication()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/disputes/1/reopen-request",
            new { reason = "The issue remains unresolved after closure." });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
