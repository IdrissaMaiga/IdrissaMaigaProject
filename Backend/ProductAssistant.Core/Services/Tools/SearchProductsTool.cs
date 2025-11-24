using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// Tool for searching products using the scraping service
/// </summary>
public class SearchProductsTool : IToolService
{
    private readonly IArukeresoScrapingService _scrapingService;
    private readonly ILogger<SearchProductsTool> _logger;

    public string Name => "search_products";
    
    public string Description => "Searches for products on e-commerce sites based on a search query. Use this when the user wants to find products, search for items, or look for specific products.";

    public ToolParameterSchema Parameters => new ToolParameterSchema
    {
        Type = "object",
        Properties = new Dictionary<string, ToolParameterProperty>
        {
            {
                "query",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "The search query or product name to search for (e.g., 'laptop', 'iPhone 15', 'wireless headphones')"
                }
            },
            {
                "maxResults",
                new ToolParameterProperty
                {
                    Type = "integer",
                    Description = "Maximum number of results to return (default: 10, max: 50)",
                    Default = 10
                }
            }
        },
        Required = new List<string> { "query" }
    };

    public SearchProductsTool(
        IArukeresoScrapingService scrapingService,
        ILogger<SearchProductsTool> logger)
    {
        _scrapingService = scrapingService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!toolCall.Arguments.TryGetValue("query", out var queryObj) || queryObj == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Query parameter is required",
                    ErrorCode = "MISSING_PARAMETER"
                };
            }

            var query = queryObj.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Query cannot be empty",
                    ErrorCode = "INVALID_PARAMETER"
                };
            }

            var maxResults = 50; // Increased default to show more products
            if (toolCall.Arguments.TryGetValue("maxResults", out var maxResultsObj))
            {
                if (int.TryParse(maxResultsObj?.ToString(), out var parsedMax) && parsedMax > 0)
                {
                    maxResults = Math.Min(parsedMax, 100); // Increased cap to 100
                }
            }

            _logger.LogInformation("Searching for products with query: {Query}, MaxResults: {MaxResults}", query, maxResults);

            var products = await _scrapingService.SearchProductsAsync(query, cancellationToken);
            
            // Limit results
            var limitedProducts = products.Take(maxResults).ToList();

            _logger.LogInformation("Found {Count} products for query: {Query}", limitedProducts.Count, query);

            // Return full Product objects (not summaries) so that:
            // 1. All product data is preserved when messages are saved to database
            // 2. UI can properly display products with URLs, images, descriptions, etc.
            // 3. Consistent with other tools
            return new ToolExecutionResult
            {
                Success = true,
                Result = new
                {
                    query = query,
                    count = limitedProducts.Count,
                    products = limitedProducts // Return full Product objects with all fields
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search_products tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error searching products: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

