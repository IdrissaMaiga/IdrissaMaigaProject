using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ShopAssistant.Services;

/// <summary>
/// Mobile client service that calls the API service for AI conversations.
/// This implements IConversationalAIService for the mobile app by making HTTP calls
/// to the backend API service's /api/chat/message endpoint.
/// </summary>
public class ConversationalAIClientService : IConversationalAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConversationalAIClientService>? _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ConversationalAIClientService(IHttpClientFactory httpClientFactory, ILogger<ConversationalAIClientService>? logger = null)
    {
        _httpClient = httpClientFactory.CreateClient("ApiService");
        _logger = logger;
    }

    /// <summary>
    /// Sends a chat message to the API service and gets AI response with products.
    /// The API service handles grounding search automatically.
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
            // Use camelCase property names to match backend expectations (configured with JsonNamingPolicy.CamelCase)
            var request = new ChatRequestDto
            {
                Message = userMessage,
                UserId = userId,
                ApiKey = apiKey,
                ConversationId = conversationId
            };

            var endpoint = "/api/chat/message";
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
            _logger?.LogInformation("Sending chat message to API service: {Url}, User: {UserId}, Conversation: {ConversationId}, HasApiKey: {HasApiKey}", 
                fullUrl, userId, conversationId, hasApiKey);

            // Serialize with camelCase naming policy to match backend
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogError("Chat API returned error: {StatusCode}, {Error}", response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponseDto>(JsonOptions, cancellationToken);
            
            if (chatResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize chat response");
            }

            _logger?.LogInformation("Received AI response with {ProductCount} products", chatResponse.Products?.Count ?? 0);

            return new ConversationalResponse
            {
                Response = chatResponse.Response,
                Products = chatResponse.Products ?? new List<Product>()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error calling chat API");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calling chat API");
            throw;
        }
    }

    /// <summary>
    /// Searches products using the API service's search endpoint.
    /// </summary>
    public async Task<List<Product>> SearchProductsFromQueryAsync(string query, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Match backend API format: { query, apiKey } (camelCase)
            var request = new SearchRequestDto
            {
                Query = query,
                ApiKey = apiKey
            };

            _logger?.LogInformation("Searching products with query: {Query}", query);

            // Serialize with camelCase naming policy to match backend
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/chat/search", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponseDto>(JsonOptions, cancellationToken);
            
            return searchResponse?.Products ?? new List<Product>();
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error calling search API");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calling search API");
            throw;
        }
    }

    /// <summary>
    /// Compares products using the API service (if available).
    /// Falls back to local comparison if API endpoint not available.
    /// </summary>
    public Task<string> CompareProductsAsync(List<Product> products, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        // For now, return a simple comparison
        // This can be enhanced to call an API endpoint if available
        if (products == null || products.Count < 2)
        {
            return Task.FromResult("Please provide at least 2 products to compare.");
        }

        var comparison = $"Comparing {products.Count} products:\n\n";
        foreach (var product in products)
        {
            comparison += $"{product.Name}: {product.Price:N0} {product.Currency}\n";
        }

        return Task.FromResult(comparison);
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

    /// <summary>
    /// Gets conversation messages from the backend API.
    /// This ensures the mobile app has the latest messages from the backend database.
    /// </summary>
    public async Task<List<ConversationMessage>> GetConversationMessagesAsync(int conversationId, int limit = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"/api/conversations/{conversationId}/messages?limit={limit}";
            _logger?.LogInformation("Fetching conversation messages from API: {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var messages = await response.Content.ReadFromJsonAsync<List<ConversationMessageDto>>(JsonOptions, cancellationToken);
            
            if (messages == null)
            {
                return new List<ConversationMessage>();
            }

            // Convert DTOs to domain models
            var result = messages.Select(m => new ConversationMessage
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                ProductId = m.ProductId,
                UserId = m.UserId,
                Message = m.Message,
                Response = m.Response,
                IsUserMessage = m.IsUserMessage,
                CreatedAt = m.CreatedAt,
                ProductIdsJson = m.ProductIdsJson,
                ProductsJson = m.ProductsJson
            }).ToList();

            _logger?.LogInformation("Fetched {Count} messages from API for conversation {ConversationId}", result.Count, conversationId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error fetching conversation messages");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching conversation messages");
            throw;
        }
    }

    private class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? ApiKey { get; set; }
        public int? ConversationId { get; set; }
    }

    private class SearchRequestDto
    {
        public string Query { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
    }

    private class ConversationMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int? ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool IsUserMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProductIdsJson { get; set; }
        public string? ProductsJson { get; set; }
    }
}

