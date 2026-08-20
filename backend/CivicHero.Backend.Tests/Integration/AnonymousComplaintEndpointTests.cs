using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class AnonymousComplaintEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnonymousComplaintEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Anonymous_submission_endpoint_should_be_public_and_reject_invalid_form()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(string.Empty), "title");
        content.Add(new StringContent(string.Empty), "description");
        content.Add(new StringContent(string.Empty), "captchaToken");

        using var response = await _client.PostAsync("/api/v1/anonymous-complaints", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_tracking_endpoint_should_be_public_and_validate_input()
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/anonymous-complaints/track", new
        {
            referenceNumber = string.Empty,
            trackingToken = string.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
