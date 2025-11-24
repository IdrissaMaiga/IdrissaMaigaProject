using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// Tool for getting all products saved by a user
/// </summary>
public class GetUserProductsTool : IToolService
{
    private readonly IProductService _productService;
    private readonly ILogger<GetUserProductsTool> _logger;

    public string Name => "get_user_products";
    
    public string Description => "Gets all products saved or associated with a specific user. Use this when the user asks about their saved products, wants to see their product list, or references 'my products'.";

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
                    Description = "The user ID to get products for. If not provided, will use the current user context."
                }
            }
        },
        Required = new List<string>()
    };

    public GetUserProductsTool(
        IProductService productService,
        ILogger<GetUserProductsTool> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            string? userId = null;
            if (toolCall.Arguments.TryGetValue("userId", out var userIdObj) && userIdObj != null)
            {
                userId = userIdObj.ToString();
            }

            _logger.LogInformation("Getting products for user: {UserId}", userId ?? "anonymous");

            var products = await _productService.GetAllProductsAsync(userId);

            _logger.LogInformation("Found {Count} products for user: {UserId}", products.Count, userId ?? "anonymous");

            return new ToolExecutionResult
            {
                Success = true,
                Result = new
                {
                    userId = userId ?? "anonymous",
                    count = products.Count,
                    products = products.Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
                        currency = p.Currency,
                        storeName = p.StoreName
                    }).ToList()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing get_user_products tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error getting user products: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

