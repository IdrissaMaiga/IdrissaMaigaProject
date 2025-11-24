using Microsoft.AspNetCore.Mvc;

namespace ProductAssistant.Api.Controllers;

/// <summary>
/// Proxy controller to fetch images from external sources (like Arukereso CDN)
/// This bypasses CORS and User-Agent restrictions that block mobile apps
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImageProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageProxyController> _logger;

    public ImageProxyController(
        IHttpClientFactory httpClientFactory,
        ILogger<ImageProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Proxies an image request through the backend to bypass CORS/User-Agent restrictions
    /// </summary>
    /// <param name="url">The image URL to fetch</param>
    /// <returns>The image file</returns>
    [HttpGet]
    public async Task<IActionResult> GetImage([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new { error = "URL parameter is required" });
        }

        try
        {
            // Create HTTP client with browser-like User-Agent to bypass restrictions
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "image/webp,image/apng,image/*,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://www.arukereso.hu/");

            _logger.LogInformation("Proxying image request: {Url}", url);

            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch image: {Url}, Status: {Status}", url, response.StatusCode);
                return NotFound(new { error = "Image not found or unavailable" });
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            _logger.LogInformation("Successfully proxied image: {Url}, Size: {Size} bytes", url, imageBytes.Length);

            // Return image with proper content type and caching headers
            Response.Headers.Add("Cache-Control", "public, max-age=86400"); // Cache for 24 hours
            return File(imageBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying image: {Url}", url);
            return StatusCode(500, new { error = "Failed to fetch image", message = ex.Message });
        }
    }
}

