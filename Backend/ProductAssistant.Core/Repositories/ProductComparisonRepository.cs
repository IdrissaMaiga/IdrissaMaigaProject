using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Models;
using System.Linq.Expressions;

namespace ProductAssistant.Core.Repositories;

public class ProductComparisonRepository : IProductComparisonRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductComparisonRepository> _logger;

    public ProductComparisonRepository(AppDbContext context, ILogger<ProductComparisonRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductComparison?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductComparisons
            .Include(pc => pc.Products)
            .FirstOrDefaultAsync(pc => pc.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ProductComparison>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductComparisons
            .Include(pc => pc.Products)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductComparison>> FindAsync(Expression<Func<ProductComparison, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.ProductComparisons
            .Include(pc => pc.Products)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductComparison> AddAsync(ProductComparison entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _context.ProductComparisons.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ProductComparison> UpdateAsync(ProductComparison entity, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(entity.Id, cancellationToken);
        if (existing == null)
            throw new InvalidOperationException($"ProductComparison with id {entity.Id} not found");

        existing.Name = entity.Name;
        existing.ComparisonAnalysis = entity.ComparisonAnalysis;
        existing.Recommendation = entity.Recommendation;
        existing.BestValueProductId = entity.BestValueProductId;
        existing.BestQualityProductId = entity.BestQualityProductId;
        existing.CheapestProductId = entity.CheapestProductId;
        existing.ComparisonMetrics = entity.ComparisonMetrics;
        existing.UpdatedAt = DateTime.UtcNow;

        // Update products if provided
        if (entity.Products != null && entity.Products.Any())
        {
            existing.Products.Clear();
            foreach (var product in entity.Products)
            {
                var dbProduct = await _context.Products.FindAsync(new object[] { product.Id }, cancellationToken);
                if (dbProduct != null)
                {
                    existing.Products.Add(dbProduct);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductComparisons.AnyAsync(pc => pc.Id == id, cancellationToken);
    }

    public async Task<List<ProductComparison>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var results = await _context.ProductComparisons
            .Include(pc => pc.Products)
            .Where(pc => pc.UserId == userId)
            .OrderByDescending(pc => pc.CreatedAt)
            .ToListAsync(cancellationToken);
        return results;
    }

    public async Task<ProductComparison> CreateAsync(ProductComparison comparison, CancellationToken cancellationToken = default)
    {
        return await AddAsync(comparison, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var comparison = await GetByIdAsync(id, cancellationToken);
        if (comparison == null)
            return false;

        _context.ProductComparisons.Remove(comparison);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

