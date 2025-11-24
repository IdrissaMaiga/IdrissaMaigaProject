namespace ProductAssistant.Core.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "HUF";
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public string? StoreName { get; set; }
    public DateTime ScrapedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UserId { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "HUF";
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public string? StoreName { get; set; }
    public string? UserId { get; set; }
}

public class UpdateProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "HUF";
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public string? StoreName { get; set; }
}

