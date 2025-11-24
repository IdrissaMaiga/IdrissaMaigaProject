using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services.Tools;

/// <summary>
/// Tool for getting detailed information about a specific product
/// </summary>
public class GetProductDetailsTool : IToolService
{
    private readonly IProductService _productService;
    private readonly ILogger<GetProductDetailsTool> _logger;

    public string Name => "get_product_details";
    
    public string Description => "Gets detailed information about a specific product by its ID. Use this when the user asks about a specific product, wants details, or needs more information about a product they've seen.";

    public ToolParameterSchema Parameters => new ToolParameterSchema
    {
        Type = "object",
        Properties = new Dictionary<string, ToolParameterProperty>
        {
            {
                "productId",
                new ToolParameterProperty
                {
                    Type = "integer",
                    Description = "The unique identifier (ID) of the product to get details for"
                }
            }
        },
        Required = new List<string> { "productId" }
    };

    public GetProductDetailsTool(
        IProductService productService,
        ILogger<GetProductDetailsTool> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!toolCall.Arguments.TryGetValue("productId", out var productIdObj) || productIdObj == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "productId parameter is required",
                    ErrorCode = "MISSING_PARAMETER"
                };
            }

            if (!int.TryParse(productIdObj.ToString(), out var productId) || productId <= 0)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "productId must be a positive integer",
                    ErrorCode = "INVALID_PARAMETER"
                };
            }

            _logger.LogInformation("Getting product details for product ID: {ProductId}", productId);

            var product = await _productService.GetProductByIdAsync(productId);

            if (product == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Product with ID {productId} not found",
                    ErrorCode = "PRODUCT_NOT_FOUND"
                };
            }

            _logger.LogInformation("Retrieved product details for: {ProductName}", product.Name);

            return new ToolExecutionResult
            {
                Success = true,
                Result = new
                {
                    id = product.Id,
                    name = product.Name,
                    price = product.Price,
                    currency = product.Currency,
                    storeName = product.StoreName,
                    imageUrl = product.ImageUrl,
                    productUrl = product.ProductUrl,
                    createdAt = product.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing get_product_details tool");
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Error getting product details: {ex.Message}",
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }
}

