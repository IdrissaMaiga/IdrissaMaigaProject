using Microsoft.AspNetCore.Mvc;
using ProductAssistant.Api.DTOs;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IConversationalAIService _aiService;
    private readonly IProductService _productService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IConversationalAIService aiService,
        IProductService productService,
        ILogger<ChatController> logger)
    {
        _aiService = aiService;
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Unified chat endpoint - AI processes message and automatically performs grounding search when needed.
    /// Products are returned inline with the conversational response.
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var userId = request.UserId ?? "anonymous";
            var contextProducts = await _productService.GetAllProductsAsync(userId);
            
            var hasApiKey = !string.IsNullOrWhiteSpace(request.ApiKey);
            _logger.LogInformation("Processing chat message for user {UserId}, conversation {ConversationId}, HasApiKey: {HasApiKey}, ApiKeyLength: {ApiKeyLength}", 
                userId, request.ConversationId, hasApiKey, request.ApiKey?.Length ?? 0);

            var response = await _aiService.GetResponseAsync(
                request.Message,
                userId,
                request.ApiKey,
                contextProducts,
                request.ConversationId,
                CancellationToken.None);

            _logger.LogInformation("AI response generated with {ProductCount} products", 
                response.Products?.Count ?? 0);

            return Ok(new ChatResponse
            {
                Response = response.Response,
                Products = response.Products ?? new List<ProductAssistant.Core.Models.Product>(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "An error occurred while processing your message", message = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to verify API key is being received correctly.
    /// </summary>
    [HttpGet("test-apikey")]
    public IActionResult TestApiKey([FromQuery] string? apiKey)
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
        return Ok(new 
        { 
            HasApiKey = hasApiKey,
            ApiKeyLength = apiKey?.Length ?? 0,
            Message = hasApiKey ? "API key received successfully" : "API key is missing"
        });
    }

    /// <summary>
    /// Direct product search endpoint (legacy support).
    /// For the unified interface, use /message endpoint which includes automatic grounding search.
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<ProductAssistant.Api.DTOs.SearchResponse>> SearchProducts([FromBody] SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            _logger.LogInformation("Direct product search: {Query}", request.Query);

            var products = await _aiService.SearchProductsFromQueryAsync(request.Query, request.ApiKey);
            
            return Ok(new DTOs.SearchResponse
            {
                Products = products ?? new List<ProductAssistant.Core.Models.Product>(),
                Count = products?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { error = "An error occurred while searching products", message = ex.Message });
        }
    }
}
