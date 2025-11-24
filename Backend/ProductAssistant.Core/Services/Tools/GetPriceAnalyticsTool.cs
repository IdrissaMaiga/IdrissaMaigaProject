using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// Advanced tool for getting price analytics and statistics
/// </summary>
public class GetPriceAnalyticsTool : IToolService
{
    private readonly IProductService _productService;
    private readonly ILogger<GetPriceAnalyticsTool> _logger;

    public string Name => "get_price_analytics";
    
    public string Description => "Gets price analytics, statistics, and trends for products. Use this when the user asks about price ranges, average prices, cheapest/most expensive products, or price statistics.";
    
    public ToolParameterSchema Parameters => new ToolParameterSchema
    {
        Type = "object",
        Properties = new Dictionary<string, ToolParameterProperty>
        {
            {
                "category",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "Category to analyze (optional, analyzes all products if not specified)"
                }
            },
            {
                "userId",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "User ID to analyze their saved products (optional)"
                }
            }
        },
        Required = new List<string>()
    };

    public GetPriceAnalyticsTool(
        IProductService productService,
        ILogger<GetPriceAnalyticsTool> logger)
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

            // Apply category filter if specified
            // Category filter removed - field no longer exists

            if (!allProducts.Any())
            {
                return new ToolExecutionResult
                {
                    Success = true,
                    Result = new
                    {
                        message = "No products found for analysis",
                        count = 0
                    }
                };
            }

            var prices = allProducts.Select(p => p.Price).ToList();
            var analytics = new
            {
                totalProducts = allProducts.Count,
                priceStatistics = new
                {
                    min = prices.Min(),
                    max = prices.Max(),
                    average = prices.Average(),
                    median = prices.OrderBy(p => p).Skip(prices.Count / 2).First(),
                    sum = prices.Sum()
                },
                cheapestProduct = allProducts.OrderBy(p => p.Price).FirstOrDefault() != null
                    ? new
                    {
                        id = allProducts.OrderBy(p => p.Price).First().Id,
                        name = allProducts.OrderBy(p => p.Price).First().Name,
                        price = allProducts.OrderBy(p => p.Price).First().Price,
                        currency = allProducts.OrderBy(p => p.Price).First().Currency
                    }
                    : null,
                mostExpensiveProduct = allProducts.OrderByDescending(p => p.Price).FirstOrDefault() != null
                    ? new
                    {
                        id = allProducts.OrderByDescending(p => p.Price).First().Id,
                        name = allProducts.OrderByDescending(p => p.Price).First().Name,
                        price = allProducts.OrderByDescending(p => p.Price).First().Price,
                        currency = allProducts.OrderByDescending(p => p.Price).First().Currency
                    }
                    : null,
                storeBreakdown = allProducts
                    .Where(p => !string.IsNullOrEmpty(p.StoreName))
                    .GroupBy(p => p.StoreName)
                    .Select(g => new
                    {
                        store = g.Key,
                        count = g.Count(),
                        avgPrice = g.Average(p => p.Price),
                        minPrice = g.Min(p => p.Price),
                        maxPrice = g.Max(p => p.Price)
                    })
                    .OrderByDescending(x => x.count)
                    .Take(10)
                    .ToList()
            };

            _logger.LogInformation("Generated price analytics for {Count} products", allProducts.Count);

            return new ToolExecutionResult
            {
                Success = true,
                Result = analytics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing get_price_analytics tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error getting price analytics: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

