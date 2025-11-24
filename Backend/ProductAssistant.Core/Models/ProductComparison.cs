namespace ProductAssistant.Core.Models;

public class ProductComparison
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UserId { get; set; }
    
    // AI-generated comparison data
    public string ComparisonAnalysis { get; set; } = string.Empty; // AI-generated detailed comparison
    public string Recommendation { get; set; } = string.Empty; // AI recommendation
    public string? BestValueProductId { get; set; } // ID of the best value product
    public string? BestQualityProductId { get; set; } // ID of the best quality product
    public string? CheapestProductId { get; set; } // ID of the cheapest product
    
    // JSON string to store comparison scores/metrics
    public string? ComparisonMetrics { get; set; }
    
    // Navigation properties - many-to-many relationship with products
    public List<Product> Products { get; set; } = new();
}

