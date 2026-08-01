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

    [Theory]
    [InlineData("/api/v1/security/session")]
    [InlineData("/api/v1/security/activity")]
    [InlineData("/api/v1/security/sessions")]
    [InlineData("/api/v1/users/profile/avatar")]
    [InlineData("/api/v1/verification/history")]
    [InlineData("/api/v1/verification/123")]
    public async Task Protected_security_get_endpoints_should_reject_anonymous_request(string endpoint)
    {
        using var response = await _client.GetAsync(endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/security/change-password", new
        {
            currentPassword = "Current@123",
            newPassword = "Different@456",
            confirmPassword = "Different@456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task Change_email_endpoint_should_reject_anonymous_request()
    {
        using var response = await _client.PostAsJsonAsync("/api/v1/users/profile/email-change", new
        {
            newEmail = "changed@example.com",
            currentPassword = "Current@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Selected_session_revoke_endpoint_should_reject_anonymous_request()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/security/sessions/example-session");
        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Theory]
    [InlineData("POST", "/api/v1/verification/123/decision")]
    [InlineData("PUT", "/api/v1/verification/123/decision")]
    [InlineData("DELETE", "/api/v1/verification/123/decision")]
    [InlineData("POST", "/api/v1/verification/123/remind")]
    [InlineData("POST", "/api/v1/verification/123/supervisor-decision")]
    public async Task Protected_verification_write_endpoints_should_reject_anonymous_request(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint)
        {
            Content = JsonContent.Create(new
            {
                decision = "Approved",
                rating = 5,
                remarks = "Verification endpoint authorization test.",
                latitude = 19.0760,
                longitude = 72.8777,
                approveCitizen = true
            })
        };

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Verification_evidence_endpoint_should_reject_anonymous_request()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0xFF, 0xD8, 0xFF]), "Evidence", "verification.jpg");
        using var response = await _client.PostAsync("/api/v1/verification/123/evidence", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Theory]
    [InlineData("POST", "/api/v1/assignments/123/escalate")]
    [InlineData("POST", "/api/v1/assignments/123/resume")]
    [InlineData("POST", "/api/v1/assignments/123/request-rework")]
    [InlineData("POST", "/api/v1/assignments/123/instructions")]
    [InlineData("POST", "/api/v1/assignments/123/request-citizen-evidence")]
    [InlineData("GET", "/api/v1/assignments/escalation-history")]
    [InlineData("GET", "/api/v1/disputes/123/history")]
    [InlineData("POST", "/api/v1/disputes/123/supervisor-decision")]
    public async Task Protected_supervisor_operations_should_reject_anonymous_request(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            request.Content = JsonContent.Create(new
            {
                reason = "Supervisor authorization test reason.",
                message = "Supervisor authorization test message.",
                decision = "CitizenCorrect",
                remarks = "Supervisor authorization test remarks."
            });
        }

        using var response = await _client.SendAsync(request);
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
