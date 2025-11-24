namespace ProductAssistant.Api.DTOs;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ApiKey { get; set; }
    public int? ConversationId { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public List<ProductAssistant.Core.Models.Product> Products { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
}

public class SearchResponse
{
    public List<ProductAssistant.Core.Models.Product> Products { get; set; } = new();
    public int Count { get; set; }
}

