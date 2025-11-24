using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// Advanced tool for filtering products by various criteria
/// </summary>
public class FilterProductsTool : IToolService
{
    private readonly IProductService _productService;
    private readonly ILogger<FilterProductsTool> _logger;

    public string Name => "filter_products";
    
    public string Description => "Filters products by price range, category, store, or other criteria. Use this when the user wants to find products within a specific price range, from a specific store, or in a particular category.";
    
    public ToolParameterSchema Parameters => new ToolParameterSchema
    {
        Type = "object",
        Properties = new Dictionary<string, ToolParameterProperty>
        {
            {
                "minPrice",
                new ToolParameterProperty
                {
                    Type = "number",
                    Description = "Minimum price filter (optional)"
                }
            },
            {
                "maxPrice",
                new ToolParameterProperty
                {
                    Type = "number",
                    Description = "Maximum price filter (optional)"
                }
            },
            {
                "category",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "Product category to filter by (optional)"
                }
            },
            {
                "storeName",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "Store name to filter by (optional)"
                }
            },
            {
                "userId",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "User ID to filter user's saved products (optional)"
                }
            }
        },
        Required = new List<string>()
    };

    public FilterProductsTool(
        IProductService productService,
        ILogger<FilterProductsTool> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            var allProducts = new List<Product>();
            string? userId = null;

            if (toolCall.Arguments.TryGetValue("userId", out var userIdObj) && userIdObj != null)
            {
                userId = userIdObj.ToString();
                allProducts = await _productService.GetAllProductsAsync(userId);
            }
            else
            {
                allProducts = await _productService.GetAllProductsAsync();
            }

            var filteredProducts = allProducts.AsQueryable();

            // Apply price filters
            if (toolCall.Arguments.TryGetValue("minPrice", out var minPriceObj))
            {
                if (decimal.TryParse(minPriceObj?.ToString(), out var minPrice))
                {
                    filteredProducts = filteredProducts.Where(p => p.Price >= minPrice);
                }
            }

            if (toolCall.Arguments.TryGetValue("maxPrice", out var maxPriceObj))
            {
                if (decimal.TryParse(maxPriceObj?.ToString(), out var maxPrice))
                {
                    filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice);
                }
            }

            // Category filter removed - field no longer exists

            // Apply store filter
            if (toolCall.Arguments.TryGetValue("storeName", out var storeObj) && storeObj != null)
            {
                var storeName = storeObj.ToString()?.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(storeName))
                {
                    filteredProducts = filteredProducts.Where(p => 
                        p.StoreName != null && p.StoreName.ToLowerInvariant().Contains(storeName));
                }
            }

            var results = filteredProducts.ToList();

            _logger.LogInformation("Filtered products: {Count} results", results.Count);

            // Return full Product objects (not summaries) so that:
            // 1. All product data is preserved when messages are saved to database
            // 2. UI can properly display products with URLs, images, descriptions, etc.
            // 3. Consistent with other tools
            return new ToolExecutionResult
            {
                Success = true,
                Result = new
                {
                    count = results.Count,
                    filters = new
                    {
                        minPrice = toolCall.Arguments.TryGetValue("minPrice", out _),
                        maxPrice = toolCall.Arguments.TryGetValue("maxPrice", out _),
                        category = toolCall.Arguments.TryGetValue("category", out _),
                        storeName = toolCall.Arguments.TryGetValue("storeName", out _)
                    },
                    products = results // Return full Product objects with all fields
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing filter_products tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error filtering products: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

