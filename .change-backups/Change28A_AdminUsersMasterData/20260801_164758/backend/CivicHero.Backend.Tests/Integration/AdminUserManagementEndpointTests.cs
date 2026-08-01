using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class AdminUserManagementEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;
    public AdminUserManagementEndpointTests(CivicHeroWebApplicationFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Theory]
    [InlineData("GET", "/api/v1/admin/user-management/users")]
    [InlineData("POST", "/api/v1/admin/user-management/citizens")]
    [InlineData("PUT", "/api/v1/admin/user-management/users/123/email")]
    [InlineData("DELETE", "/api/v1/admin/user-management/users/123")]
    [InlineData("POST", "/api/v1/admin/user-management/users/123/restore")]
    [InlineData("GET", "/api/v1/admin/user-management/users/123/history")]
    [InlineData("GET", "/api/v1/admin/user-management/users/123/role-history")]
    [InlineData("GET", "/api/v1/admin/user-management/department-heads")]
    [InlineData("PUT", "/api/v1/admin/user-management/department-heads/1")]
    [InlineData("DELETE", "/api/v1/admin/user-management/department-heads/1")]
    [InlineData("GET", "/api/v1/admin/user-management/master-data")]
    [InlineData("PUT", "/api/v1/admin/user-management/master-data")]
    public async Task Endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (method is "POST" or "PUT" or "DELETE")
        {
            request.Content = JsonContent.Create(new
            {
                fullName = "Authorization Test Citizen",
                email = "authorization.test@example.com",
                newEmail = "authorization.changed@example.com",
                temporaryPassword = "Secure@123",
                markEmailVerified = true,
                markVerified = true,
                reason = "Authorization test operation reason.",
                userId = 1,
                priorities = Array.Empty<object>(),
                statuses = Array.Empty<object>(),
                featureFlags = Array.Empty<object>(),
                authorities = Array.Empty<object>()
            });
        }
        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
