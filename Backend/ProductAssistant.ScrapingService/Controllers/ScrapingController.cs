using Microsoft.AspNetCore.Mvc;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.ScrapingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScrapingController : ControllerBase
{
    private readonly IArukeresoScrapingService _scrapingService;
    private readonly ILogger<ScrapingController> _logger;

    public ScrapingController(IArukeresoScrapingService scrapingService, ILogger<ScrapingController> logger)
    {
        _scrapingService = scrapingService;
        _logger = logger;
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<Product>>> SearchProducts([FromBody] SearchRequest request)
    {
        try
        {
            var products = await _scrapingService.SearchProductsAsync(request.SearchTerm);
            return Ok(new SearchResponse
            {
                Products = products,
                Count = products.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { error = "Error searching products", message = ex.Message });
        }
    }

    [HttpPost("details")]
    public async Task<ActionResult<Product>> GetProductDetails([FromBody] ProductDetailsRequest request)
    {
        try
        {
            var product = await _scrapingService.GetProductDetailsAsync(request.ProductUrl);
            if (product == null)
                return NotFound();
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product details");
            return StatusCode(500, new { error = "Error getting product details", message = ex.Message });
        }
    }

    [HttpPost("category")]
    public async Task<ActionResult<List<Product>>> ScrapeCategory([FromBody] CategoryRequest request)
    {
        try
        {
            var products = await _scrapingService.ScrapeCategoryAsync(request.CategoryUrl);
            return Ok(new SearchResponse
            {
                Products = products,
                Count = products.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping category");
            return StatusCode(500, new { error = "Error scraping category", message = ex.Message });
        }
    }
}

public class SearchRequest
{
    public string SearchTerm { get; set; } = string.Empty;
}

public class ProductDetailsRequest
{
    public string ProductUrl { get; set; } = string.Empty;
}

public class CategoryRequest
{
    public string CategoryUrl { get; set; } = string.Empty;
}

public class SearchResponse
{
    public List<Product> Products { get; set; } = new();
    public int Count { get; set; }
}

