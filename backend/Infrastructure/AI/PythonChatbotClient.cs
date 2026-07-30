using System.Net.Http.Json;
using CivicHero.Backend.Core.DTOs.Chat;
using Microsoft.Extensions.Logging;
using System.Text.Json; // Added for JsonException

namespace CivicHero.Backend.Infrastructure.AI
{
    public class PythonChatbotClient : IPythonChatbotClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonChatbotClient> _logger;

        public PythonChatbotClient(HttpClient httpClient, ILogger<PythonChatbotClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PythonChatResponseDto> SendMessageAsync(PythonChatRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/v1/chat/generate", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<PythonChatResponseDto>();
                return result ?? new PythonChatResponseDto
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize response from Python service."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to Python chatbot service failed.");
                return new PythonChatResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Failed to connect to Python chatbot service: {ex.Message}"
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON response from Python chatbot service.");
                return new PythonChatResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Invalid response from Python chatbot service: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while communicating with Python chatbot service.");
                return new PythonChatResponseDto
                {
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }
    }
}