namespace ProductAssistant.Api.DTOs;

public class CreateConversationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = "New Conversation";
}

public class UpdateConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

public class ConversationResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ConversationMessageResponse
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

