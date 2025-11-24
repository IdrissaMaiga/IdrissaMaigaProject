using Microsoft.AspNetCore.Mvc;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.AIService.Controllers;

/// <summary>
/// AI Service controller for AI-powered conversations with grounding search.
/// This service performs automatic product searches (grounding) when search intent is detected
/// during conversation, and returns products inline with AI responses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IConversationalAIService _aiService;
    private readonly ILogger<AIController> _logger;

    public AIController(IConversationalAIService aiService, ILogger<AIController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Processes chat message with automatic grounding search.
    /// The AI automatically detects search intent and performs product searches,
    /// returning products inline with the conversational response.
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            _logger.LogInformation("Processing AI chat message for user: {UserId}, Conversation: {ConversationId}", 
                request.UserId, request.ConversationId);

            var response = await _aiService.GetResponseAsync(
                request.Message,
                request.UserId ?? "anonymous",
                request.ApiKey,
                request.ContextProducts,
                request.ConversationId,
                CancellationToken.None);

            _logger.LogInformation("AI response generated with {ProductCount} products", 
                response.Products?.Count ?? 0);

            return Ok(new ChatResponse
            {
                Response = response.Response,
                Products = response.Products ?? new List<Product>(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "Error processing chat", message = ex.Message });
        }
    }

    /// <summary>
    /// Direct product search endpoint.
    /// For unified interface, use /chat endpoint which includes automatic grounding search.
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<SearchResponse>> SearchProducts([FromBody] SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query cannot be empty" });
            }

            _logger.LogInformation("Direct product search: {Query}", request.Query);

            // Note: apiKey not available in this endpoint, will use fallback extraction
            var products = await _aiService.SearchProductsFromQueryAsync(request.Query, null);
            
            return Ok(new SearchResponse
            {
                Products = products,
                Count = products.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { error = "Error searching products", message = ex.Message });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ApiKey { get; set; }
    public List<Product>? ContextProducts { get; set; }
    public int? ConversationId { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
}

public class SearchResponse
{
    public List<Product> Products { get; set; } = new();
    public int Count { get; set; }
}

