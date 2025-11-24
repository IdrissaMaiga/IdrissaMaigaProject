using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public interface IProductComparisonService
{
    /// <summary>
    /// Generates an AI-based comparison of multiple products
    /// </summary>
    Task<ProductComparison> GenerateComparisonAsync(
        List<int> productIds, 
        string? userId = null, 
        string? apiKey = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all comparisons for a user
    /// </summary>
    Task<List<ProductComparison>> GetUserComparisonsAsync(string userId);
    
    /// <summary>
    /// Gets a comparison by ID
    /// </summary>
    Task<ProductComparison?> GetComparisonByIdAsync(int comparisonId);
    
    /// <summary>
    /// Deletes a comparison
    /// </summary>
    Task<bool> DeleteComparisonAsync(int comparisonId);
    
    /// <summary>
    /// Updates an existing comparison with fresh AI analysis
    /// </summary>
    Task<ProductComparison> RefreshComparisonAsync(
        int comparisonId, 
        string? apiKey = null,
        CancellationToken cancellationToken = default);
}

