using System.Net;
using System.Net.Http.Json;

namespace CivicHero.Backend.Tests.Integration;

public sealed class ChatbotHistoryEndpointTests : IClassFixture<CivicHeroWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChatbotHistoryEndpointTests(CivicHeroWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("POST", "/api/v1/chatbot/session")]
    [InlineData("GET", "/api/v1/chatbot/sessions")]
    [InlineData("GET", "/api/v1/chatbot/session/example-session")]
    [InlineData("PUT", "/api/v1/chatbot/session/example-session/title")]
    [InlineData("POST", "/api/v1/chatbot/session/example-session/continue")]
    [InlineData("POST", "/api/v1/chatbot/message")]
    [InlineData("POST", "/api/v1/chatbot/message/stream")]
    [InlineData("POST", "/api/v1/chatbot/session/example-session/messages/1/feedback")]
    [InlineData("DELETE", "/api/v1/chatbot/session/example-session")]
    [InlineData("DELETE", "/api/v1/chatbot/sessions/example-session")]
    public async Task Chatbot_management_endpoints_should_reject_anonymous_requests(string method, string endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            request.Content = JsonContent.Create(new
            {
                sessionId = "example-session",
                message = "How do I report a pothole?",
                title = "Road issue guidance",
                helpful = true,
                comment = "The response was clear."
            });
        }

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
