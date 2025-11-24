using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Services;

/// <summary>
/// Client service that calls the AI service via HTTP for the API service.
/// This implements IConversationalAIService for the API service by making HTTP calls
/// to the AI service's /api/ai/chat endpoint.
/// </summary>
public class AIServiceClient : IConversationalAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIServiceClient> _logger;

    public AIServiceClient(HttpClient httpClient, ILogger<AIServiceClient> logger)
    {
        // HttpClient is configured in AddProductAssistantHttpClients with base address
        // Base address should be set to AI service URL (e.g., http://ai-service:8080)
        // We append /api here for the endpoint path
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends a chat message to the AI service and gets response with products.
    /// The AI service performs grounding search automatically.
    /// </summary>
    public async Task<ConversationalResponse> GetResponseAsync(
        string userMessage, 
        string userId, 
        string? apiKey = null, 
        List<Product>? contextProducts = null, 
        int? conversationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                Message = userMessage,
                UserId = userId,
                ApiKey = apiKey,
                ContextProducts = contextProducts,
                ConversationId = conversationId
            };

            _logger.LogInformation("Calling AI service for user: {UserId}, Conversation: {ConversationId}", userId, conversationId);

            var response = await _httpClient.PostAsJsonAsync("/api/ai/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponseDto>(cancellationToken: cancellationToken);
            
            if (chatResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize AI service response");
            }

            _logger.LogInformation("AI service returned response with {ProductCount} products", chatResponse.Products?.Count ?? 0);

            return new ConversationalResponse
            {
                Response = chatResponse.Response,
                Products = chatResponse.Products ?? new List<Product>()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling AI service");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI service");
            throw;
        }
    }

    /// <summary>
    /// Searches products using the AI service's search endpoint.
    /// </summary>
    public async Task<List<Product>> SearchProductsFromQueryAsync(string query, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { Query = query };

            _logger.LogInformation("Searching products via AI service: {Query}", query);

            var response = await _httpClient.PostAsJsonAsync("/api/ai/search", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponseDto>(cancellationToken: cancellationToken);
            
            return searchResponse?.Products ?? new List<Product>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling AI service search");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI service search");
            throw;
        }
    }

    /// <summary>
    /// Compares products using the AI service (if available).
    /// Falls back to simple comparison if not available.
    /// </summary>
    public async Task<string> CompareProductsAsync(List<Product> products, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        // For now, return a simple comparison
        // This can be enhanced to call an AI service endpoint if available
        if (products == null || products.Count < 2)
        {
            return "Please provide at least 2 products to compare.";
        }

        var comparison = $"Comparing {products.Count} products:\n\n";
        foreach (var product in products)
        {
            comparison += $"{product.Name}: {product.Price:N0} {product.Currency}\n";
        }

        return comparison;
    }

    private class ChatResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public List<Product> Products { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    private class SearchResponseDto
    {
        public List<Product> Products { get; set; } = new();
        public int Count { get; set; }
    }
}
