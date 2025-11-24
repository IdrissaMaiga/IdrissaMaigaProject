using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Repositories;

public interface IProductComparisonRepository
{
    Task<ProductComparison?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ProductComparison>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ProductComparison> CreateAsync(ProductComparison comparison, CancellationToken cancellationToken = default);
    Task<ProductComparison> UpdateAsync(ProductComparison comparison, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}

