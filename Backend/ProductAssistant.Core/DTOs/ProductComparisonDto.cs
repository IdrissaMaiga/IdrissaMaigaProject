namespace ProductAssistant.Core.DTOs;

public class ProductComparisonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UserId { get; set; }
    public string ComparisonAnalysis { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? BestValueProductId { get; set; }
    public string? BestQualityProductId { get; set; }
    public string? CheapestProductId { get; set; }
    public string? ComparisonMetrics { get; set; }
    public List<ProductDto> Products { get; set; } = new();
}

public class CreateProductComparisonDto
{
    public string Name { get; set; } = string.Empty;
    public List<int> ProductIds { get; set; } = new();
    public string? UserId { get; set; }
}

