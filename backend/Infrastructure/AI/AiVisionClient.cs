using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CivicHero.Backend.Core.DTOs.AI;
using CivicHero.Backend.Core.Interfaces;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Client for communicating with the AI vision microservice via HTTP.
/// </summary>
public class AiVisionClient : IAiVisionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _visionServiceUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiVisionClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="visionServiceUrl">The base URL of the vision microservice.</param>
    public AiVisionClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _visionServiceUrl = configuration["VisionService:Url"] ??
                           throw new InvalidOperationException("VisionService:Url configuration is missing");
    }

    /// <summary>
    /// Compares a complaint image with a resolution image to determine if the issue appears resolved.
    /// </summary>
    /// <param name="complaintImageBytes">The bytes of the complaint image.</param>
    /// <param name="resolutionImageBytes">The bytes of the resolution image.</param>
    /// <param name="complaintDescription">Optional description of the complaint for context.</param>
    /// <returns>The vision analysis result.</returns>
    public async Task<VisionComparisonResult> CompareImagesAsync(byte[] complaintImageBytes, byte[] resolutionImageBytes, string? complaintDescription = null)
    {
        if (complaintImageBytes == null) throw new ArgumentNullException(nameof(complaintImageBytes));
        if (resolutionImageBytes == null) throw new ArgumentNullException(nameof(resolutionImageBytes));

        try
        {
            using var content = new MultipartFormDataContent();

            // Add the images
            content.Add(new ByteArrayContent(complaintImageBytes), "complaint_image", "complaint.jpg");
            content.Add(new ByteArrayContent(resolutionImageBytes), "resolution_image", "resolution.jpg");

            // Add optional description
            if (!string.IsNullOrWhiteSpace(complaintDescription))
            {
                content.Add(new StringContent(complaintDescription), "complaint_description");
            }

            var response = await _httpClient.PostAsync($"{_visionServiceUrl}/api/v1/vision/compare-resolution", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Vision service returned error: {response.StatusCode} - {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VisionComparisonResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize vision service response");
            }

            return result;
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error communicating with vision service: {ex.Message}", ex);
        }
    }
}