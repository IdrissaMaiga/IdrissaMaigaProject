using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// AI-driven tool for getting personalized product recommendations based on conversation context
/// </summary>
public class GetProductRecommendationsTool : IToolService
{
    private readonly IProductService _productService;
    private readonly ILLMService _llmService;
    private readonly ILogger<GetProductRecommendationsTool> _logger;

    public string Name => "get_product_recommendations";
    
    public string Description => "Gets AI-powered personalized product recommendations based on the conversation context, user preferences, budget, and needs. Use this when the user asks for recommendations, suggestions, 'what should I buy', or needs help deciding between products. The AI will analyze the conversation and recommend multiple relevant products.";
    
    public ToolParameterSchema Parameters => new ToolParameterSchema
    {
        Type = "object",
        Properties = new Dictionary<string, ToolParameterProperty>
        {
            {
                "userId",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "User ID to get personalized recommendations (required for context)"
                }
            },
            {
                "conversationContext",
                new ToolParameterProperty
                {
                    Type = "string",
                    Description = "Summary of what the user is looking for based on the conversation (e.g., 'budget laptop for students', 'gaming phone under 200000 HUF', 'wireless headphones for commuting')"
                }
            },
            {
                "maxPrice",
                new ToolParameterProperty
                {
                    Type = "number",
                    Description = "Maximum price mentioned by user (optional)"
                }
            },
            {
                "minPrice",
                new ToolParameterProperty
                {
                    Type = "number",
                    Description = "Minimum price mentioned by user (optional)"
                }
            },
            {
                "limit",
                new ToolParameterProperty
                {
                    Type = "integer",
                    Description = "Number of recommendations to return (default: 5, max: 10)",
                    Default = 5
                }
            }
        },
        Required = new List<string> { "userId", "conversationContext" }
    };

    public GetProductRecommendationsTool(
        IProductService productService,
        ILLMService llmService,
        ILogger<GetProductRecommendationsTool> logger)
    {
        _productService = productService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract parameters
            var userId = toolCall.Arguments.TryGetValue("userId", out var userIdObj) 
                ? userIdObj?.ToString() 
                : null;

            var conversationContext = toolCall.Arguments.TryGetValue("conversationContext", out var contextObj) 
                ? contextObj?.ToString() 
                : "general recommendations";

            var limit = 5;
            if (toolCall.Arguments.TryGetValue("limit", out var limitObj))
            {
                if (int.TryParse(limitObj?.ToString(), out var parsedLimit) && parsedLimit > 0)
                {
                    limit = Math.Min(parsedLimit, 10);
                }
            }

            decimal? maxPrice = null;
            if (toolCall.Arguments.TryGetValue("maxPrice", out var maxPriceObj))
            {
                if (decimal.TryParse(maxPriceObj?.ToString(), out var parsedMaxPrice))
                {
                    maxPrice = parsedMaxPrice;
                }
            }

            decimal? minPrice = null;
            if (toolCall.Arguments.TryGetValue("minPrice", out var minPriceObj))
            {
                if (decimal.TryParse(minPriceObj?.ToString(), out var parsedMinPrice))
                {
                    minPrice = parsedMinPrice;
                }
            }

            _logger.LogInformation("Getting AI recommendations for user {UserId} with context: {Context}, price range: {MinPrice}-{MaxPrice}", 
                userId, conversationContext, minPrice, maxPrice);

            // Get all available products
            var allProducts = await _productService.GetAllProductsAsync(userId);

            // Apply price filters if specified
            var filteredProducts = allProducts.AsEnumerable();
            if (maxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value);
            }
            if (minPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value);
            }

            var productsToAnalyze = filteredProducts.ToList();

            if (!productsToAnalyze.Any())
            {
                _logger.LogWarning("No products available for recommendations");
                return new ToolExecutionResult
                {
                    Success = true,
                    Result = new
                    {
                        count = 0,
                        message = "No products found matching the criteria",
                        recommendations = new List<object>()
                    }
                };
            }

            // Return all products for AI to analyze and recommend
            // The AI will intelligently select and explain the best matches
            // IMPORTANT: Return full Product objects (not summaries) so that:
            // 1. All product data is preserved when messages are saved to database
            // 2. UI can properly display products with URLs, images, descriptions, etc.
            // 3. Consistent with other tools (SearchProductsTool, FilterProductsTool)
            _logger.LogInformation("Returning {Count} full product objects for AI-based recommendation analysis", productsToAnalyze.Count);

            return new ToolExecutionResult
            {
                Success = true,
                Result = new
                {
                    count = productsToAnalyze.Count,
                    userId = userId,
                    context = conversationContext,
                    priceRange = new
                    {
                        min = minPrice,
                        max = maxPrice
                    },
                    products = productsToAnalyze, // Return full Product objects with all fields
                    message = $"Found {productsToAnalyze.Count} products matching the criteria. Analyze these products based on the conversation context '{conversationContext}' and recommend the best {limit} options with detailed explanations for each recommendation."
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing get_product_recommendations tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error getting recommendations: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

