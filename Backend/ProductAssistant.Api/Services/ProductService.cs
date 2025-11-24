using ProductAssistant.Core.Models;
using ProductAssistant.Core.Repositories;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllProductsAsync(string? userId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (await _repository.GetAllAsync()).ToList();
            }
            else
            {
                return (await _repository.GetByUserIdAsync(userId)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        try
        {
            return await _repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by id: {Id}", id);
            throw;
        }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        try
        {
            return await _repository.AddAsync(product);
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
            await _repository.UpdateAsync(product);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with id: {Id}", product.Id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            await _repository.DeleteAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with id: {Id}", id);
            return false;
        }
    }

    public async Task<List<Product>> SearchProductsAsync(string searchTerm)
    {
        try
        {
            return (await _repository.SearchAsync(searchTerm)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<ProductComparison> CompareProductsAsync(List<int> productIds, string? userId = null, string? apiKey = null)
    {
        // This method should delegate to ProductComparisonService
        // Keeping it for interface compatibility but it should be refactored
        throw new NotImplementedException("Use IProductComparisonService.CompareProductsAsync instead");
    }
}

