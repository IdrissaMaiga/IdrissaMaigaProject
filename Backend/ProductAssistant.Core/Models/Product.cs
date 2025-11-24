using System.Text.Json.Serialization;

namespace ProductAssistant.Core.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "HUF";
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public string? StoreName { get; set; }
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    
    // Navigation properties - ignore in JSON serialization to prevent circular references
    [JsonIgnore]
    public List<ConversationMessage> Messages { get; set; } = new();
}





