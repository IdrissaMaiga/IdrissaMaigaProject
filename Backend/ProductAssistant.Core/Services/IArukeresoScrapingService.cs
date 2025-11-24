using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public interface IArukeresoScrapingService
{
    Task<List<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Product?> GetProductDetailsAsync(string productUrl, CancellationToken cancellationToken = default);
    Task<List<Product>> ScrapeCategoryAsync(string categoryUrl, CancellationToken cancellationToken = default);
}





