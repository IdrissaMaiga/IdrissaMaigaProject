using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync(string? userId = null);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);
    Task<List<Product>> SearchProductsAsync(string searchTerm);
    Task<ProductComparison> CompareProductsAsync(List<int> productIds, string? userId = null, string? apiKey = null);
}

