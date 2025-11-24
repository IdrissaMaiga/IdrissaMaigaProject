using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ShopAssistant.Services;

/// <summary>
/// Mobile client service that calls the API service for conversation management.
/// This implements IConversationMemoryService for the mobile app by making HTTP calls
/// to the backend API service's /api/conversations endpoints.
/// </summary>
public class ConversationMemoryClientService : IConversationMemoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConversationMemoryClientService>? _logger;

    public ConversationMemoryClientService(IHttpClientFactory httpClientFactory, ILogger<ConversationMemoryClientService>? logger = null)
    {
        _httpClient = httpClientFactory.CreateClient("ApiService");
        _logger = logger;
    }

    public async Task<List<Conversation>> GetConversationsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger?.LogWarning("GetConversationsAsync called with empty userId");
                return new List<Conversation>();
            }

            var endpoint = $"/api/conversations?userId={Uri.EscapeDataString(userId)}";
            _logger?.LogInformation("Getting conversations for user: {UserId}", userId);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError("Get conversations API returned error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<Conversation>();
                }
                
                response.EnsureSuccessStatusCode();
            }

            var conversations = await response.Content.ReadFromJsonAsync<List<Conversation>>();
            _logger?.LogInformation("Retrieved {Count} conversations for user: {UserId}", conversations?.Count ?? 0, userId);
            
            return conversations ?? new List<Conversation>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting conversations for user: {UserId}", userId);
            return new List<Conversation>();
        }
    }

    public async Task<Conversation> CreateConversationAsync(string userId, string title = "New Conversation")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId is required", nameof(userId));
            }

            // Match backend API format: { userId, title } (camelCase)
            var request = new { userId = userId, title = title };
            var endpoint = "/api/conversations";
            _logger?.LogInformation("Creating conversation for user: {UserId}, title: {Title}", userId, title);

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            response.EnsureSuccessStatusCode();

            var conversation = await response.Content.ReadFromJsonAsync<Conversation>();
            if (conversation == null)
            {
                throw new InvalidOperationException("Failed to deserialize conversation response");
            }

            _logger?.LogInformation("Created conversation: {ConversationId} for user: {UserId}", conversation.Id, userId);
            return conversation;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating conversation for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<Conversation?> GetConversationByIdAsync(int conversationId)
    {
        try
        {
            var endpoint = $"/api/conversations/{conversationId}";
            _logger?.LogInformation("Getting conversation: {ConversationId}", conversationId);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var conversation = await response.Content.ReadFromJsonAsync<Conversation>();
            return conversation;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting conversation: {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task UpdateConversationTitleAsync(int conversationId, string title)
    {
        try
        {
            // Match backend API format: { title } (camelCase)
            var request = new { title = title };
            var endpoint = $"/api/conversations/{conversationId}/title";
            _logger?.LogInformation("Updating conversation title: {ConversationId}, title: {Title}", conversationId, title);

            var response = await _httpClient.PutAsJsonAsync(endpoint, request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating conversation title: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task DeleteConversationAsync(int conversationId)
    {
        try
        {
            var endpoint = $"/api/conversations/{conversationId}";
            _logger?.LogInformation("Deleting conversation: {ConversationId}", conversationId);

            var response = await _httpClient.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting conversation: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(int conversationId, int limit = 10)
    {
        try
        {
            var endpoint = $"/api/conversations/{conversationId}/messages?limit={limit}";
            _logger?.LogInformation("Getting conversation history: {ConversationId}, limit: {Limit}", conversationId, limit);

            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError("Get conversation history API returned error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<ConversationMessage>();
                }
                
                response.EnsureSuccessStatusCode();
            }

            // Read response content once
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger?.LogInformation("Get conversation history API response (first 500 chars): {ResponseContent}", 
                responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0)) ?? "null");
            
            // Parse from the string we already read with case-insensitive options
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var history = System.Text.Json.JsonSerializer.Deserialize<List<ConversationMessage>>(responseContent, options);
            _logger?.LogInformation("Retrieved {Count} messages for conversation: {ConversationId}", history?.Count ?? 0, conversationId);
            
            if (history != null && history.Any())
            {
                foreach (var msg in history.Take(3))
                {
                    _logger?.LogInformation("  Message {MessageId}: IsUser={IsUser}, Message='{Message}', Response='{Response}'", 
                        msg.Id, msg.IsUserMessage, 
                        msg.Message?.Substring(0, Math.Min(50, msg.Message?.Length ?? 0)) ?? "", 
                        msg.Response?.Substring(0, Math.Min(50, msg.Response?.Length ?? 0)) ?? "");
                }
            }
            else
            {
                _logger?.LogWarning("WARNING: API returned empty message list for conversation {ConversationId}. Response was: {ResponseContent}", 
                    conversationId, responseContent?.Substring(0, Math.Min(200, responseContent?.Length ?? 0)) ?? "null");
            }
            
            return history ?? new List<ConversationMessage>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting conversation history: {ConversationId}", conversationId);
            return new List<ConversationMessage>();
        }
    }

    public async Task SaveMessageAsync(ConversationMessage message)
    {
        try
        {
            // Match backend API format: { userId, message, response, isUserMessage, productId, productIdsJson, productsJson } (camelCase)
            var request = new
            {
                userId = message.UserId,
                message = message.Message,
                response = message.Response,
                isUserMessage = message.IsUserMessage,
                productId = message.ProductId,
                productIdsJson = message.ProductIdsJson,
                productsJson = message.ProductsJson
            };

            var endpoint = $"/api/conversations/{message.ConversationId}/messages";
            _logger?.LogInformation("Saving message to conversation: {ConversationId}, IsUserMessage: {IsUserMessage}, UserId: {UserId}, Message: '{Message}'", 
                message.ConversationId, message.IsUserMessage, request.userId, 
                request.message?.Substring(0, Math.Min(50, request.message?.Length ?? 0)) ?? "");

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError("Save message API returned error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }
            
            // Read response content once
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger?.LogInformation("Save message API response: {ResponseContent}", 
                responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0)) ?? "null");

            // Parse from the string we already read with case-insensitive options
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var savedMessage = System.Text.Json.JsonSerializer.Deserialize<ConversationMessage>(responseContent, options);
            if (savedMessage != null)
            {
                message.Id = savedMessage.Id;
                message.CreatedAt = savedMessage.CreatedAt;
                _logger?.LogInformation("Message saved successfully: MessageId={MessageId}, ConversationId={ConversationId}, CreatedAt={CreatedAt}", 
                    savedMessage.Id, savedMessage.ConversationId, savedMessage.CreatedAt);
            }
            else
            {
                _logger?.LogWarning("WARNING: Save message API returned success but could not deserialize response. Response was: {ResponseContent}", 
                    responseContent?.Substring(0, Math.Min(200, responseContent?.Length ?? 0)) ?? "null");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving message to conversation: {ConversationId}", message.ConversationId);
            throw;
        }
    }

    public async Task<string> GetConversationContextAsync(int conversationId)
    {
        try
        {
            // For now, get history and build context
            // This could be optimized with a dedicated endpoint if needed
            var history = await GetConversationHistoryAsync(conversationId, 10);
            
            if (!history.Any())
                return string.Empty;

            var context = new System.Text.StringBuilder();
            context.AppendLine("Previous conversation context:");
            
            foreach (var message in history)
            {
                var role = message.IsUserMessage ? "User" : "Assistant";
                var content = message.IsUserMessage ? message.Message : message.Response;
                context.AppendLine($"{role}: {content}");
            }
            
            return context.ToString();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting conversation context: {ConversationId}", conversationId);
            return string.Empty;
        }
    }

    // Legacy methods (deprecated)
    [Obsolete("Use GetConversationHistoryAsync(int conversationId) instead")]
    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId, int limit = 10)
    {
        try
        {
            // Get the most recent conversation for this user
            var conversations = await GetConversationsAsync(userId);
            var conversation = conversations.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();

            if (conversation == null)
            {
                // Create a default conversation if none exists
                conversation = await CreateConversationAsync(userId, "Main Conversation");
            }

            return await GetConversationHistoryAsync(conversation.Id, limit);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting conversation history for user: {UserId}", userId);
            return new List<ConversationMessage>();
        }
    }

    [Obsolete("Use DeleteConversationAsync(int conversationId) instead")]
    public async Task ClearConversationAsync(string userId)
    {
        try
        {
            var conversations = await GetConversationsAsync(userId);
            foreach (var conversation in conversations)
            {
                await DeleteConversationAsync(conversation.Id);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing conversations for user: {UserId}", userId);
            throw;
        }
    }

    [Obsolete("Use GetConversationContextAsync(int conversationId) instead")]
    public async Task<string> GetConversationContextAsync(string userId)
    {
        try
        {
            var conversations = await GetConversationsAsync(userId);
            var conversation = conversations.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();

            if (conversation == null)
                return string.Empty;

            return await GetConversationContextAsync(conversation.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting conversation context for user: {UserId}", userId);
            return string.Empty;
        }
    }
}
