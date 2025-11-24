using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public interface IConversationalAIService
{
    Task<ConversationalResponse> GetResponseAsync(string userMessage, string userId, string? apiKey = null, List<Product>? contextProducts = null, int? conversationId = null, CancellationToken cancellationToken = default);
    Task<List<Product>> SearchProductsFromQueryAsync(string query, string? apiKey = null, CancellationToken cancellationToken = default);
    Task<string> CompareProductsAsync(List<Product> products, string? apiKey = null, CancellationToken cancellationToken = default);
}

public class ConversationalResponse
{
    public string Response { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();
}

