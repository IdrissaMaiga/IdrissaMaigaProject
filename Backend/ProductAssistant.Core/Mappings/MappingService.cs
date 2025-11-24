using ProductAssistant.Core.DTOs;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Mappings;

public class MappingService : IMappingService
{
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return source switch
        {
            Product product => (TDestination)(object)MapToDto(product),
            ProductDto dto => (TDestination)(object)MapToEntity(dto),
            CreateProductDto dto => (TDestination)(object)MapToEntity(dto),
            UpdateProductDto dto => (TDestination)(object)MapToEntity(dto),
            ProductComparison comparison => (TDestination)(object)MapToDto(comparison),
            _ => throw new NotSupportedException($"Mapping from {typeof(TSource).Name} to {typeof(TDestination).Name} is not supported")
        };
    }

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        return source.Select(Map<TSource, TDestination>).ToList();
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Currency = product.Currency,
            ImageUrl = product.ImageUrl,
            ProductUrl = product.ProductUrl,
            StoreName = product.StoreName,
            ScrapedAt = product.ScrapedAt,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            UserId = product.UserId
        };
    }

    private static Product MapToEntity(ProductDto dto)
    {
        return new Product
        {
            Id = dto.Id,
            Name = dto.Name,
            Price = dto.Price,
            Currency = dto.Currency,
            ImageUrl = dto.ImageUrl,
            ProductUrl = dto.ProductUrl,
            StoreName = dto.StoreName,
            CreatedAt = dto.CreatedAt,
            UserId = dto.UserId
        };
    }

    private static Product MapToEntity(CreateProductDto dto)
    {
        return new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Currency = dto.Currency,
            ImageUrl = dto.ImageUrl,
            ProductUrl = dto.ProductUrl,
            StoreName = dto.StoreName,
            UserId = dto.UserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Product MapToEntity(UpdateProductDto dto)
    {
        return new Product
        {
            Id = dto.Id,
            Name = dto.Name,
            Price = dto.Price,
            Currency = dto.Currency,
            ImageUrl = dto.ImageUrl,
            ProductUrl = dto.ProductUrl,
            StoreName = dto.StoreName
        };
    }

    private static ProductComparisonDto MapToDto(ProductComparison comparison)
    {
        return new ProductComparisonDto
        {
            Id = comparison.Id,
            Name = comparison.Name,
            CreatedAt = comparison.CreatedAt,
            UpdatedAt = comparison.UpdatedAt,
            UserId = comparison.UserId,
            ComparisonAnalysis = comparison.ComparisonAnalysis,
            Recommendation = comparison.Recommendation,
            BestValueProductId = comparison.BestValueProductId,
            BestQualityProductId = comparison.BestQualityProductId,
            CheapestProductId = comparison.CheapestProductId,
            ComparisonMetrics = comparison.ComparisonMetrics,
            Products = comparison.Products?.Select(MapToDto).ToList() ?? new List<ProductDto>()
        };
    }
}

