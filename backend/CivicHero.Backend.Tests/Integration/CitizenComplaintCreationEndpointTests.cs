using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class CitizenComplaintCreationEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CitizenComplaintCreationEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Complaint_creation_with_extended_evidence_should_reject_anonymous_request()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Road damage near school"), "Title");
        content.Add(new StringContent("A deep road crack is creating a safety risk for students and vehicles."), "Description");
        content.Add(new StringContent("Road Damage"), "Category");
        content.Add(new StringContent("High"), "CitizenSeverity");
        content.Add(new StringContent("1"), "DepartmentId");
        content.Add(new StringContent("1"), "WardId");
        content.Add(new StringContent("19.076"), "Latitude");
        content.Add(new StringContent("72.8777"), "Longitude");
        content.Add(new StringContent("School Road"), "Address");
        content.Add(new StringContent("Near the main gate"), "Landmark");
        content.Add(new ByteArrayContent([0x25, 0x50, 0x44, 0x46]), "Evidence", "supporting.pdf");

        using var response = await _client.PostAsync("/api/v1/complaints", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Remove_submitted_evidence_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.DeleteAsync("/api/v1/complaints/123/images/456");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Pre_submission_review_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/complaints/pre-submission-review", new
        {
            title = "Road damage near school",
            description = "A deep road crack is creating a safety risk for students and vehicles.",
            category = "Road Damage",
            latitude = 19.076,
            longitude = 72.8777
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
