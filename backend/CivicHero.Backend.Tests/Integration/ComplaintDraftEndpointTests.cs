using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class ComplaintDraftEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ComplaintDraftEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Current_draft_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.GetAsync("/api/v1/complaint-drafts/current");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Save_draft_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.PutAsJsonAsync("/api/v1/complaint-drafts/current", new
        {
            title = "Road damage near school",
            citizenSeverity = "High"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Draft_evidence_endpoint_should_reject_anonymous_request()
    {
        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        bytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(bytes, "Evidence", "proof.pdf");

        using var response = await _client.PostAsync("/api/v1/complaint-drafts/current/evidence", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
