using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Repositories;
using System.Text.Json;

namespace ProductAssistant.Core.Services;

public class ProductComparisonService : IProductComparisonService
{
    private readonly IProductComparisonRepository _comparisonRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILLMService _llmService;
    private readonly ILogger<ProductComparisonService> _logger;

    private const string ComparisonSystemPrompt = @"You are an expert product comparison assistant. Your task is to analyze and compare products objectively.

When comparing products, consider:
1. Price and value for money
2. Quality indicators (description, store reputation)
3. Features and specifications
4. Overall recommendation

Provide a structured comparison in the following format:
- Detailed Analysis: A comprehensive comparison of all products
- Best Value: Which product offers the best value for money
- Best Quality: Which product appears to be the highest quality
- Cheapest: Which product is the most affordable
- Recommendation: Your overall recommendation with reasoning

Be objective, clear, and helpful. Format your response in a way that's easy to read.";

    public ProductComparisonService(
        IProductComparisonRepository comparisonRepository,
        IProductRepository productRepository,
        ILLMService llmService,
        ILogger<ProductComparisonService> logger)
    {
        _comparisonRepository = comparisonRepository;
        _productRepository = productRepository;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<ProductComparison> GenerateComparisonAsync(
        List<int> productIds, 
        string? userId = null, 
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        if (productIds == null || productIds.Count < 2)
        {
            throw new ArgumentException("At least 2 products are required for comparison", nameof(productIds));
        }

        _logger.LogInformation("Generating AI comparison for {Count} products", productIds.Count);

        // Fetch all products from database
        var products = new List<Product>();
        foreach (var id in productIds)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product != null)
            {
                products.Add(product);
            }
        }

        if (products.Count < 2)
        {
            throw new InvalidOperationException("Could not retrieve at least 2 products for comparison");
        }

        // Generate AI comparison
        string comparisonAnalysis;
        string recommendation;
        string? bestValueId = null;
        string? bestQualityId = null;
        string? cheapestId = null;

        if (!string.IsNullOrWhiteSpace(apiKey) && await _llmService.IsAvailableAsync(apiKey))
        {
            try
            {
                var prompt = BuildComparisonPrompt(products);
                var aiResponse = await _llmService.GenerateResponseAsync(
                    prompt,
                    apiKey,
                    ComparisonSystemPrompt,
                    null,
                    cancellationToken);

                // Parse AI response to extract structured data
                var parsed = ParseAIResponse(aiResponse, products);
                comparisonAnalysis = parsed.Analysis;
                recommendation = parsed.Recommendation;
                bestValueId = parsed.BestValueProductId;
                bestQualityId = parsed.BestQualityProductId;
                cheapestId = parsed.CheapestProductId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI comparison, falling back to rule-based");
                var fallback = GenerateFallbackComparison(products);
                comparisonAnalysis = fallback.Analysis;
                recommendation = fallback.Recommendation;
                bestValueId = fallback.BestValueProductId;
                bestQualityId = fallback.BestQualityProductId;
                cheapestId = fallback.CheapestProductId;
            }
        }
        else
        {
            _logger.LogWarning("LLM service not available, using rule-based comparison");
            var fallback = GenerateFallbackComparison(products);
            comparisonAnalysis = fallback.Analysis;
            recommendation = fallback.Recommendation;
            bestValueId = fallback.BestValueProductId;
            bestQualityId = fallback.BestQualityProductId;
            cheapestId = fallback.CheapestProductId;
        }

        // Calculate comparison metrics
        var metrics = CalculateMetrics(products);
        var metricsJson = JsonSerializer.Serialize(metrics);

        // Create comparison entity
        var comparison = new ProductComparison
        {
            Name = $"Comparison: {string.Join(", ", products.Select(p => p.Name).Take(3))}",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ComparisonAnalysis = comparisonAnalysis,
            Recommendation = recommendation,
            BestValueProductId = bestValueId,
            BestQualityProductId = bestQualityId,
            CheapestProductId = cheapestId,
            ComparisonMetrics = metricsJson,
            Products = products
        };

        var created = await _comparisonRepository.CreateAsync(comparison, cancellationToken);

        _logger.LogInformation("Created comparison with ID: {ComparisonId}", created.Id);
        return created;
    }

    public async Task<List<ProductComparison>> GetUserComparisonsAsync(string userId)
    {
        return await _comparisonRepository.GetByUserIdAsync(userId);
    }

    public async Task<ProductComparison?> GetComparisonByIdAsync(int comparisonId)
    {
        return await _comparisonRepository.GetByIdAsync(comparisonId);
    }

    public async Task<bool> DeleteComparisonAsync(int comparisonId)
    {
        return await _comparisonRepository.DeleteAsync(comparisonId);
    }

    public async Task<ProductComparison> RefreshComparisonAsync(
        int comparisonId, 
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        var comparison = await GetComparisonByIdAsync(comparisonId);
        if (comparison == null)
        {
            throw new InvalidOperationException($"Comparison with ID {comparisonId} not found");
        }

        if (comparison.Products.Count < 2)
        {
            throw new InvalidOperationException("Comparison must have at least 2 products");
        }

        // Regenerate AI comparison
        string comparisonAnalysis;
        string recommendation;
        string? bestValueId = null;
        string? bestQualityId = null;
        string? cheapestId = null;

        if (!string.IsNullOrWhiteSpace(apiKey) && await _llmService.IsAvailableAsync(apiKey))
        {
            try
            {
                var prompt = BuildComparisonPrompt(comparison.Products);
                var aiResponse = await _llmService.GenerateResponseAsync(
                    prompt,
                    apiKey,
                    ComparisonSystemPrompt,
                    null,
                    cancellationToken);

                var parsed = ParseAIResponse(aiResponse, comparison.Products);
                comparisonAnalysis = parsed.Analysis;
                recommendation = parsed.Recommendation;
                bestValueId = parsed.BestValueProductId;
                bestQualityId = parsed.BestQualityProductId;
                cheapestId = parsed.CheapestProductId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing AI comparison");
                throw;
            }
        }
        else
        {
            var fallback = GenerateFallbackComparison(comparison.Products);
            comparisonAnalysis = fallback.Analysis;
            recommendation = fallback.Recommendation;
            bestValueId = fallback.BestValueProductId;
            bestQualityId = fallback.BestQualityProductId;
            cheapestId = fallback.CheapestProductId;
        }

        // Update comparison
        comparison.ComparisonAnalysis = comparisonAnalysis;
        comparison.Recommendation = recommendation;
        comparison.BestValueProductId = bestValueId;
        comparison.BestQualityProductId = bestQualityId;
        comparison.CheapestProductId = cheapestId;
        comparison.UpdatedAt = DateTime.UtcNow;
        comparison.ComparisonMetrics = JsonSerializer.Serialize(CalculateMetrics(comparison.Products));

        return await _comparisonRepository.UpdateAsync(comparison, cancellationToken);
    }

    private string BuildComparisonPrompt(List<Product> products)
    {
        var prompt = "Please compare the following products in detail:\n\n";
        
        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            prompt += $"Product {i + 1} (ID: {product.Id}):\n";
            prompt += $"  Name: {product.Name}\n";
            prompt += $"  Price: {product.Price:N0} {product.Currency}\n";
            if (!string.IsNullOrEmpty(product.StoreName))
            {
                prompt += $"  Store: {product.StoreName}\n";
            }
            prompt += "\n";
        }

        prompt += "\nPlease provide:\n";
        prompt += "1. A detailed analysis comparing all products\n";
        prompt += "2. Which product offers the best value for money (mention Product ID)\n";
        prompt += "3. Which product appears to be the highest quality (mention Product ID)\n";
        prompt += "4. Which product is the cheapest (mention Product ID)\n";
        prompt += "5. Your overall recommendation with reasoning";

        return prompt;
    }

    private (string Analysis, string Recommendation, string? BestValueProductId, string? BestQualityProductId, string? CheapestProductId) 
        ParseAIResponse(string aiResponse, List<Product> products)
    {
        // Try to extract structured information from AI response
        var analysis = aiResponse;
        var recommendation = "";
        string? bestValueId = null;
        string? bestQualityId = null;
        string? cheapestId = null;

        // Look for product IDs in the response
        foreach (var product in products)
        {
            var idStr = product.Id.ToString();
            var lowerResponse = aiResponse.ToLowerInvariant();
            
            if (lowerResponse.Contains($"best value") && lowerResponse.Contains(idStr))
            {
                bestValueId = idStr;
            }
            if (lowerResponse.Contains($"best quality") && lowerResponse.Contains(idStr))
            {
                bestQualityId = idStr;
            }
            if (lowerResponse.Contains($"cheapest") && lowerResponse.Contains(idStr))
            {
                cheapestId = idStr;
            }
        }

        // Extract recommendation section if present
        var recommendationIndex = aiResponse.IndexOf("recommendation", StringComparison.OrdinalIgnoreCase);
        if (recommendationIndex >= 0)
        {
            recommendation = aiResponse.Substring(recommendationIndex).Trim();
        }
        else
        {
            recommendation = "Based on the analysis above, consider your specific needs and budget when making a decision.";
        }

        // If IDs weren't found, use fallback logic
        if (string.IsNullOrEmpty(bestValueId))
        {
            bestValueId = products.OrderByDescending(p => p.Price > 0 ? p.Price : decimal.MaxValue).First().Id.ToString();
        }
        if (string.IsNullOrEmpty(cheapestId))
        {
            cheapestId = products.OrderBy(p => p.Price).First().Id.ToString();
        }
        if (string.IsNullOrEmpty(bestQualityId))
        {
            bestQualityId = products.OrderByDescending(p => p.Price).First().Id.ToString();
        }

        return (analysis, recommendation, bestValueId, bestQualityId, cheapestId);
    }

    private (string Analysis, string Recommendation, string? BestValueProductId, string? BestQualityProductId, string? CheapestProductId) 
        GenerateFallbackComparison(List<Product> products)
    {
        var cheapest = products.OrderBy(p => p.Price).First();
        var mostExpensive = products.OrderByDescending(p => p.Price).First();
        var bestValue = products.OrderByDescending(p => p.Price > 0 ? 1.0m / p.Price : 0).First();

        var analysis = $"Comparison of {products.Count} products:\n\n";
        foreach (var product in products.OrderBy(p => p.Price))
        {
            analysis += $"• {product.Name}: {product.Price:N0} {product.Currency}";
            if (!string.IsNullOrEmpty(product.StoreName))
            {
                analysis += $" ({product.StoreName})";
            }
            analysis += "\n";
        }

        analysis += $"\nCheapest: {cheapest.Name} at {cheapest.Price:N0} {cheapest.Currency}";
        analysis += $"\nMost Expensive: {mostExpensive.Name} at {mostExpensive.Price:N0} {mostExpensive.Currency}";

        var recommendation = $"For best value, consider {bestValue.Name}. For the lowest price, choose {cheapest.Name}.";

        return (analysis, recommendation, bestValue.Id.ToString(), mostExpensive.Id.ToString(), cheapest.Id.ToString());
    }

    private Dictionary<string, object> CalculateMetrics(List<Product> products)
    {
        var metrics = new Dictionary<string, object>
        {
            ["ProductCount"] = products.Count,
            ["PriceRange"] = new
            {
                Min = products.Min(p => p.Price),
                Max = products.Max(p => p.Price),
                Average = products.Average(p => p.Price)
            },
            ["Stores"] = products.Where(p => !string.IsNullOrEmpty(p.StoreName))
                .Select(p => p.StoreName)
                .Distinct()
                .ToList()
        };

        return metrics;
    }
}

