using Microsoft.AspNetCore.Mvc;
using ProductAssistant.Core.Models;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductComparisonsController : ControllerBase
{
    private readonly IProductComparisonService _comparisonService;
    private readonly ILogger<ProductComparisonsController> _logger;

    public ProductComparisonsController(
        IProductComparisonService comparisonService,
        ILogger<ProductComparisonsController> logger)
    {
        _comparisonService = comparisonService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ProductComparison>> CreateComparison([FromBody] CreateComparisonRequest request)
    {
        try
        {
            if (request.ProductIds == null || request.ProductIds.Count < 2)
            {
                return BadRequest("At least 2 products are required for comparison");
            }

            var comparison = await _comparisonService.GenerateComparisonAsync(
                request.ProductIds,
                request.UserId,
                request.ApiKey,
                CancellationToken.None);

            return CreatedAtAction(nameof(GetComparison), new { id = comparison.Id }, comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product comparison");
            return StatusCode(500, new { error = "Error creating comparison", message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductComparison>> GetComparison(int id)
    {
        var comparison = await _comparisonService.GetComparisonByIdAsync(id);
        if (comparison == null)
            return NotFound();
        return Ok(comparison);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<ProductComparison>>> GetUserComparisons(string userId)
    {
        var comparisons = await _comparisonService.GetUserComparisonsAsync(userId);
        return Ok(comparisons);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComparison(int id)
    {
        var result = await _comparisonService.DeleteComparisonAsync(id);
        if (!result)
            return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/refresh")]
    public async Task<ActionResult<ProductComparison>> RefreshComparison(int id, [FromBody] RefreshComparisonRequest? request = null)
    {
        try
        {
            var comparison = await _comparisonService.RefreshComparisonAsync(
                id,
                request?.ApiKey,
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
}

public class CreateComparisonRequest
{
    public List<int> ProductIds { get; set; } = new();
    public string? UserId { get; set; }
    public string? ApiKey { get; set; }
}

public class RefreshComparisonRequest
{
    public string? ApiKey { get; set; }
}

