using Microsoft.AspNetCore.Mvc;
using ProductAssistant.Api.DTOs;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductComparisonService _comparisonService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService, 
        IProductComparisonService comparisonService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _comparisonService = comparisonService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetProducts([FromQuery] string? userId = null)
    {
        _logger.LogInformation("GET /api/products - UserId: {UserId}", userId ?? "NULL");
        var products = await _productService.GetAllProductsAsync(userId);
        _logger.LogInformation("GET /api/products - Returning {Count} products for UserId: {UserId}", products.Count, userId ?? "NULL");
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        _logger.LogInformation("POST /api/products - Creating product: Name={Name}, UserId={UserId}, Id={Id}", 
            product.Name, product.UserId ?? "NULL", product.Id);
        var created = await _productService.CreateProductAsync(product);
        _logger.LogInformation("POST /api/products - Created product with Id={Id}, UserId={UserId}", 
            created.Id, created.UserId ?? "NULL");
        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();
        
        var updated = await _productService.UpdateProductAsync(product);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (!result)
            return NotFound();
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<Product>>> SearchProducts([FromQuery] string term)
    {
        var products = await _productService.SearchProductsAsync(term);
        return Ok(products);
    }

    [HttpPost("compare")]
    public async Task<ActionResult<ProductComparison>> CompareProducts([FromBody] CompareProductsRequest request)
    {
        try
        {
            if (request.ProductIds == null || request.ProductIds.Count < 2)
            {
                return BadRequest(new { error = "At least 2 products are required for comparison" });
            }

            var comparison = await _comparisonService.GenerateComparisonAsync(
                request.ProductIds,
                request.UserId,
                request.ApiKey,
                CancellationToken.None);
            
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing products");
            return StatusCode(500, new { error = "Error comparing products", message = ex.Message });
        }
    }

    [HttpGet("comparisons")]
    public async Task<ActionResult<List<ProductComparison>>> GetUserComparisons([FromQuery] string? userId = null)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "UserId is required" });
        }

        var comparisons = await _comparisonService.GetUserComparisonsAsync(userId);
        return Ok(comparisons);
    }

    [HttpGet("comparisons/{id}")]
    public async Task<ActionResult<ProductComparison>> GetComparison(int id)
    {
        var comparison = await _comparisonService.GetComparisonByIdAsync(id);
        if (comparison == null)
        {
            return NotFound();
        }
        return Ok(comparison);
    }

    [HttpPost("comparisons/{id}/refresh")]
    public async Task<ActionResult<ProductComparison>> RefreshComparison(int id, [FromBody] RefreshComparisonRequest request)
    {
        try
        {
            var comparison = await _comparisonService.RefreshComparisonAsync(
                id,
                request.ApiKey,
                CancellationToken.None);
            return Ok(comparison);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing comparison");
            return StatusCode(500, new { error = "Error refreshing comparison", message = ex.Message });
        }
    }

    [HttpDelete("comparisons/{id}")]
    public async Task<IActionResult> DeleteComparison(int id)
    {
        var result = await _comparisonService.DeleteComparisonAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}





