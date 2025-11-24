using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using System.Net.Http.Json;

namespace ProductAssistant.Core.Services;

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        // BaseAddress should be configured via HttpClient registration (e.g., in MauiProgram)
        // Only set default if not already configured (for backward compatibility)
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("http://localhost:5000/api/products");
        }
        _logger = logger;
    }

    public async Task<List<Product>> GetAllProductsAsync(string? userId = null)
    {
        try
        {
            var url = string.IsNullOrEmpty(userId) ? "/api/products" : $"/api/products?userId={userId}";
            var products = await _httpClient.GetFromJsonAsync<List<Product>>(url);
            return products ?? new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Product>($"/api/products/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by id: {Id}", id);
            return null;
        }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/products", product);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<Product>();
            
            _logger.LogInformation("Product created with id: {Id}", created?.Id);
            return created ?? product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            throw;
        }
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/products/{product.Id}", product);
            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<Product>();
            
            _logger.LogInformation("Product updated with id: {Id}", product.Id);
            return updated ?? product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product");
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/products/{id}");
            
            _logger.LogInformation("Product deleted with id: {Id}", id);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product");
            return false;
        }
    }

    public async Task<List<Product>> SearchProductsAsync(string searchTerm)
    {
        try
        {
            var products = await _httpClient.GetFromJsonAsync<List<Product>>($"/api/products/search?term={Uri.EscapeDataString(searchTerm)}");
            return products ?? new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return new List<Product>();
        }
    }

    public async Task<ProductComparison> CompareProductsAsync(List<int> productIds, string? userId = null, string? apiKey = null)
    {
        try
        {
            var request = new { ProductIds = productIds, UserId = userId, ApiKey = apiKey };
            var response = await _httpClient.PostAsJsonAsync("/compare", request);
            response.EnsureSuccessStatusCode();
            var comparison = await response.Content.ReadFromJsonAsync<ProductComparison>();
            
            _logger.LogInformation("Product comparison created with id: {Id}", comparison?.Id);
            return comparison ?? throw new InvalidOperationException("Failed to create comparison");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing products");
            throw;
        }
    }
}
