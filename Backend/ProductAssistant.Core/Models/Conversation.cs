namespace ProductAssistant.Core.Models;

public class Conversation
{
    public int Id { get; set; }
    public string Title { get; set; } = "New Conversation";
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public List<ConversationMessage> Messages { get; set; } = new();
}


