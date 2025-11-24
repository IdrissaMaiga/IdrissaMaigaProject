using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// Tool for comparing multiple products
/// </summary>
public class CompareProductsTool : IToolService
{
    private readonly IProductService _productService;
    private readonly ILogger<CompareProductsTool> _logger;

    public string Name => "compare_products";
    
    public string Description => "Compares multiple products by their IDs. Use this when the user wants to compare products, see price differences, or get recommendations between products. Requires at least 2 product IDs.";

    public ToolParameterSchema Parameters => new ToolParameterSchema
    {
        Type = "object",
        Properties = new Dictionary<string, ToolParameterProperty>
        {
            {
                "productIds",
                new ToolParameterProperty
                {
                    Type = "array",
                    Description = "Array of product IDs (integers) to compare (minimum 2, maximum 5 products). Example: [1, 2, 3]"
                }
            }
        },
        Required = new List<string> { "productIds" }
    };

    public CompareProductsTool(
        IProductService productService,
        ILogger<CompareProductsTool> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!toolCall.Arguments.TryGetValue("productIds", out var productIdsObj) || productIdsObj == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "productIds parameter is required",
                    ErrorCode = "MISSING_PARAMETER"
                };
            }

            List<int> productIds;
            if (productIdsObj is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                productIds = jsonElement.EnumerateArray()
                    .Select(e => e.GetInt32())
                    .Where(id => id > 0)
                    .ToList();
            }
            else if (productIdsObj is List<object> list)
            {
                productIds = list
                    .Select(obj => Convert.ToInt32(obj))
                    .Where(id => id > 0)
                    .ToList();
            }
            else
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "productIds must be an array of integers",
                    ErrorCode = "INVALID_PARAMETER"
                };
            }

            if (productIds.Count < 2)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "At least 2 product IDs are required for comparison",
                    ErrorCode = "INVALID_PARAMETER"
                };
            }

            if (productIds.Count > 5)
            {
                productIds = productIds.Take(5).ToList();
                _logger.LogWarning("Product IDs list truncated to 5 items");
            }

            _logger.LogInformation("Comparing {Count} products: {ProductIds}", productIds.Count, string.Join(", ", productIds));

            var products = new List<Product>();
            foreach (var id in productIds)
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product != null)
                {
                    products.Add(product);
                }
            }

            if (products.Count < 2)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Could not find at least 2 valid products to compare",
                    ErrorCode = "INSUFFICIENT_PRODUCTS"
                };
            }

            // Build comparison data
            var comparison = new
            {
                productCount = products.Count,
                products = products.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    currency = p.Currency,
                    storeName = p.StoreName
                }).ToList(),
                priceRange = new
                {
                    min = products.Min(p => p.Price),
                    max = products.Max(p => p.Price),
                    average = products.Average(p => p.Price)
                },
                cheapest = products.OrderBy(p => p.Price).First().Id,
                mostExpensive = products.OrderByDescending(p => p.Price).First().Id
            };

            _logger.LogInformation("Successfully compared {Count} products", products.Count);

            return new ToolExecutionResult
            {
                Success = true,
                Result = comparison
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing compare_products tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error comparing products: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

