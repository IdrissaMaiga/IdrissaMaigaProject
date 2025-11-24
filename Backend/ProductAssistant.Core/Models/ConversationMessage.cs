namespace ProductAssistant.Core.Models;

public class ConversationMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int? ProductId { get; set; } // Deprecated - use ProductIdsJson instead
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// JSON array of product IDs associated with this message (for assistant responses with multiple products)
    /// </summary>
    public string? ProductIdsJson { get; set; }
    /// <summary>
    /// JSON array of full product data associated with this message (for assistant responses with products from AI search)
    /// This stores the complete product information so products can be displayed even if they're not saved to the database
    /// </summary>
    public string? ProductsJson { get; set; }
    
    // Navigation properties
    public Conversation? Conversation { get; set; }
    public Product? Product { get; set; }
}




