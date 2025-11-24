namespace ProductAssistant.Api.DTOs;

public class CompareProductsRequest
{
    public List<int> ProductIds { get; set; } = new();
    public string? UserId { get; set; }
    public string? ApiKey { get; set; }
}

public class RefreshComparisonRequest
{
    public string? ApiKey { get; set; }
}

