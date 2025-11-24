using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProductAssistant.Core.Services;

public class ArukeresoScrapingService : IArukeresoScrapingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArukeresoScrapingService> _logger;

    public ArukeresoScrapingService(HttpClient httpClient, ILogger<ArukeresoScrapingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // BaseAddress should be set in DI configuration (MauiProgram.cs)
        // Timeout is also set in DI, but ensure minimum timeout
        if (_httpClient.Timeout < TimeSpan.FromSeconds(30))
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
    }

    public async Task<List<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calling Docker scraping service for: {SearchTerm}", searchTerm);
            
            var request = new { SearchTerm = searchTerm };
            var response = await _httpClient.PostAsJsonAsync("/api/scraping/search", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Scraping service error: {StatusCode} - {Error}", response.StatusCode, error);
                return new List<Product>();
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken);
            var products = result?.Products ?? new List<Product>();
            
            _logger.LogInformation("Found {Count} products for: {SearchTerm}", products.Count, searchTerm);
            return products;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to scraping service. Ensure Docker is running.");
            return new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products for: {SearchTerm}", searchTerm);
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductDetailsAsync(string productUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calling Docker service for product details");
            
            var request = new { ProductUrl = productUrl };
            var response = await _httpClient.PostAsJsonAsync("/api/scraping/details", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch product details: {StatusCode}", response.StatusCode);
                return null;
            }

            var product = await response.Content.ReadFromJsonAsync<Product>(cancellationToken);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product details");
            return null;
        }
    }

    public async Task<List<Product>> ScrapeCategoryAsync(string categoryUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calling Docker service for category scraping");
            
            var request = new { CategoryUrl = categoryUrl };
            var response = await _httpClient.PostAsJsonAsync("/api/scraping/category", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to scrape category: {StatusCode}", response.StatusCode);
                return new List<Product>();
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken);
            var products = result?.Products ?? new List<Product>();
            
            _logger.LogInformation("Scraped {Count} products from category", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping category");
            return new List<Product>();
        }
    }
}

// Response DTO
public class SearchResponse
{
    public List<Product> Products { get; set; } = new();
    public int Count { get; set; }
}




